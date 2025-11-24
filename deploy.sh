#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Expense Management System Deployment Script ===${NC}"
echo ""

# Configuration
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
DEPLOY_GENAI=false

# Get admin info for SQL
echo -e "${YELLOW}Getting your Azure AD information for SQL Server admin...${NC}"
ADMIN_USER=$(az account show --query user.name -o tsv)
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

echo "Admin User: $ADMIN_USER"
echo "Admin Object ID: $ADMIN_OBJECT_ID"
echo ""

# Create resource group
echo -e "${YELLOW}Creating resource group...${NC}"
az group create --name $RESOURCE_GROUP --location $LOCATION --output table

# Deploy infrastructure
echo -e "${YELLOW}Deploying infrastructure (this may take 5-10 minutes)...${NC}"
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infrastructure/main.bicep \
  --parameters \
    adminLogin="$ADMIN_USER" \
    adminObjectId="$ADMIN_OBJECT_ID" \
    deployGenAI=$DEPLOY_GENAI \
  --output json)

# Extract outputs
echo -e "${GREEN}Extracting deployment outputs...${NC}"
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.databaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceUrl.value')

echo "App Service: $APP_SERVICE_NAME"
echo "SQL Server: $SQL_SERVER_FQDN"
echo "Database: $DATABASE_NAME"
echo "Managed Identity: $MANAGED_IDENTITY_NAME"
echo ""

# Configure App Service settings
echo -e "${YELLOW}Configuring App Service settings...${NC}"
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};" \
  --output table

# Wait for SQL Server to be fully ready
echo -e "${YELLOW}Waiting 30 seconds for SQL Server to be fully ready...${NC}"
sleep 30

# Add current user's IP to firewall
echo -e "${YELLOW}Adding your IP to SQL Server firewall...${NC}"
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $(echo $SQL_SERVER_FQDN | cut -d'.' -f1) \
  --name AllowDeployerIP \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP \
  --output table

# Install Python dependencies
echo -e "${YELLOW}Installing Python dependencies...${NC}"
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual values
echo -e "${YELLOW}Updating Python scripts with actual server and database values...${NC}"
sed -i.bak "s/SERVER = \"example.database.windows.net\"/SERVER = \"${SQL_SERVER_FQDN}\"/" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/SERVER = \"example.database.windows.net\"/SERVER = \"${SQL_SERVER_FQDN}\"/" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/SERVER = \"example.database.windows.net\"/SERVER = \"${SQL_SERVER_FQDN}\"/" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
echo -e "${YELLOW}Updating script.sql with managed identity name...${NC}"
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Import database schema
echo -e "${YELLOW}Importing database schema...${NC}"
python3 run-sql.py

# Configure database roles for managed identity
echo -e "${YELLOW}Configuring database roles for managed identity...${NC}"
python3 run-sql-dbrole.py

# Create stored procedures
echo -e "${YELLOW}Creating stored procedures...${NC}"
python3 run-sql-stored-procs.py

# Build and deploy application
echo -e "${YELLOW}Building application...${NC}"
cd app
dotnet publish -c Release -o ./publish

# Create deployment zip (files at root, not in subdirectory)
echo -e "${YELLOW}Creating deployment package...${NC}"
cd ./publish
zip -r ../../app.zip . > /dev/null
cd ../..

# Deploy to App Service
echo -e "${YELLOW}Deploying application to Azure App Service...${NC}"
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip \
  --type zip \
  --output table

# Display completion message
echo ""
echo -e "${GREEN}=== Deployment Complete ===${NC}"
echo ""
echo -e "${GREEN}Application URL: ${APP_SERVICE_URL}/Index${NC}"
echo -e "${YELLOW}Note: Navigate to ${APP_SERVICE_URL}/Index (not just the root URL)${NC}"
echo ""
echo -e "${YELLOW}To run locally:${NC}"
echo "1. Update appsettings.json connection string to use 'Authentication=Active Directory Default'"
echo "2. Run 'az login' to authenticate"
echo "3. Run 'dotnet run' from the app directory"
echo ""
