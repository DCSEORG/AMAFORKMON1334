# Expense Management System - Deployment Guide

## Overview

This is a modern, cloud-native expense management system built with ASP.NET Core 8.0 and deployed to Azure. The application demonstrates app modernization using Azure best practices including:

- Managed Identity for secure authentication
- Entra ID (Azure AD) for database access
- Infrastructure as Code with Bicep
- Optional AI-powered chat assistant with GPT-4o

## Prerequisites

- Azure CLI installed and logged in (`az login`)
- Azure subscription with appropriate permissions
- .NET 8.0 SDK (for local development)
- Python 3.x with pip (for database setup scripts)
- ODBC Driver 18 for SQL Server

## Quick Start

### Option 1: Deploy Without AI Chat (Faster)

```bash
chmod +x deploy.sh
./deploy.sh
```

This deploys:
- App Service with managed identity
- Azure SQL Database with Entra ID authentication
- Web application with expense management features

**Deployment time**: ~10 minutes

### Option 2: Deploy With AI Chat (Full Features)

```bash
chmod +x deploy-with-chat.sh
./deploy-with-chat.sh
```

This deploys everything from Option 1 plus:
- Azure OpenAI with GPT-4o model
- Azure Cognitive Search for RAG
- AI-powered chat assistant

**Deployment time**: ~15 minutes

## Accessing the Application

After deployment completes, you'll see output like:

```
Application URL: https://app-expensemgmt-xxxxx.azurewebsites.net/Index
```

**Important**: Navigate to `/Index` (not just the root URL)

### Features

1. **View Expenses** (`/Index`) - View all expenses with filtering
2. **Add Expense** (`/AddExpense`) - Create new expense entries
3. **Approve Expenses** (`/ApproveExpenses`) - Manager approval workflow
4. **AI Chat** (`/Chat`) - Natural language interface (if deployed with chat)
5. **API Docs** (`/swagger`) - Interactive API documentation

## Local Development

To run locally:

1. Update `app/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=tcp:YOUR-SERVER.database.windows.net,1433;Database=Northwind;Authentication=Active Directory Default;"
     }
   }
   ```

2. Authenticate with Azure:
   ```bash
   az login
   ```

3. Run the application:
   ```bash
   cd app
   dotnet run
   ```

4. Open browser to `https://localhost:5001/Index`

## Database Schema

The database includes:
- **Roles**: Employee, Manager
- **Users**: System users with role assignments
- **ExpenseCategories**: Travel, Meals, Supplies, etc.
- **ExpenseStatus**: Draft, Submitted, Approved, Rejected
- **Expenses**: Main expense records

All database operations use stored procedures for security and consistency.

## API Endpoints

### Expenses
- `GET /api/expenses` - Get all expenses
- `GET /api/expenses/{id}` - Get expense by ID
- `GET /api/expenses/status/{status}` - Filter by status
- `GET /api/expenses/pending` - Get pending approvals
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/status` - Update status
- `POST /api/expenses/{id}/approve` - Approve expense

### Lookups
- `GET /api/categories` - Get expense categories
- `GET /api/statuses` - Get expense statuses

### Chat (if GenAI deployed)
- `POST /api/chat` - Send chat message
- `GET /api/chat/status` - Check if GenAI is configured

## Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed architecture diagram and component descriptions.

## Error Handling

The application includes comprehensive error handling:
- Database connection failures show dummy data with error banner
- Detailed error messages with file and line numbers
- Managed identity troubleshooting guidance
- Graceful degradation when GenAI services unavailable

## Cleanup

To delete all Azure resources:

```bash
az group delete --name rg-expensemgmt-demo --yes --no-wait
```

## Troubleshooting

### Database Connection Issues

If you see managed identity errors:

1. Ensure the managed identity has database roles:
   ```sql
   CREATE USER [mid-AppModAssist-xxxxx] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [mid-AppModAssist-xxxxx];
   ALTER ROLE db_datawriter ADD MEMBER [mid-AppModAssist-xxxxx];
   ```

2. Check that `run-sql-dbrole.py` completed successfully during deployment

### GenAI Not Working

If AI chat shows errors:

1. Verify deployment used `deploy-with-chat.sh`
2. Check App Service configuration has OpenAI settings:
   ```bash
   az webapp config appsettings list --resource-group rg-expensemgmt-demo --name <app-name>
   ```

3. Confirm managed identity has "Cognitive Services OpenAI User" role on OpenAI resource

## Security Considerations

This is a **proof-of-concept** deployment. For production:

1. Enable private endpoints for SQL Database
2. Use Azure Key Vault for any additional secrets
3. Implement proper authentication and authorization
4. Enable Azure Monitor and Application Insights
5. Configure backup and disaster recovery
6. Review and apply network security groups
7. Enable Azure Defender for SQL

## License

See [LICENSE](./LICENSE) file for details.
