# App-Mod-Assist

A project to show how GitHub Copilot agent can turn screenshots of legacy apps into working proof-of-concepts for cloud native Azure replacements if the legacy database schema is also provided.

## ✨ What This Demonstrates

This repository showcases automated app modernization where:
1. Legacy application screenshots + database schema are provided as inputs
2. GitHub Copilot agent generates a complete, modern cloud-native application
3. The result is a production-ready proof-of-concept deployed to Azure

## 🎯 Generated Application Features

The modernized Expense Management System includes:

- **Modern UI**: Clean, responsive Razor Pages interface
- **REST APIs**: Complete API layer with Swagger documentation
- **Azure Integration**: App Service, SQL Database, Optional OpenAI
- **Security**: Managed Identity, Entra ID authentication, no stored credentials
- **Infrastructure as Code**: Complete Bicep templates
- **AI Assistant**: Optional GPT-4o powered chat interface
- **Error Handling**: Graceful fallback with detailed error messages

## 🚀 Quick Deployment

### Prerequisites
- Azure CLI installed and logged in
- Azure subscription with contributor access
- Basic familiarity with command line

### Deploy the Application

**Option 1: Basic Deployment (10 minutes)**
```bash
chmod +x deploy.sh
./deploy.sh
```

**Option 2: With AI Chat (15 minutes)**
```bash
chmod +x deploy-with-chat.sh
./deploy-with-chat.sh
```

After deployment, navigate to the URL shown (make sure to go to `/Index` page).

## 📖 Documentation

- **[DEPLOYMENT.md](./DEPLOYMENT.md)** - Complete deployment and usage guide
- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - System architecture and component details

## 🏗️ What Gets Created

### Infrastructure
- App Service (S1 tier) with .NET 8.0
- Azure SQL Database (Basic tier) with Entra ID auth
- User-Assigned Managed Identity
- Optional: Azure OpenAI (GPT-4o) in Sweden
- Optional: Azure Cognitive Search (Basic tier)

### Application
- **View Expenses**: Filter and browse all expense records
- **Add Expense**: Submit new expenses with categories
- **Approve Expenses**: Manager workflow for approvals  
- **AI Chat**: Natural language interface for expense operations
- **API Layer**: RESTful APIs for all operations

## 🔒 Security Features

- ✅ Entra ID (Azure AD) only authentication for SQL
- ✅ Managed Identity - no credentials in code
- ✅ HTTPS enforced for all traffic
- ✅ Role-based database access
- ✅ CodeQL security scanning passed
- ✅ Azure best practices implemented

## 💻 Local Development

```bash
# 1. Update connection string in app/appsettings.json
# 2. Login to Azure
az login

# 3. Run the app
cd app
dotnet run

# 4. Open browser to https://localhost:5001/Index
```

## 🧪 Technology Stack

- **Backend**: ASP.NET Core 8.0 (LTS)
- **Frontend**: Razor Pages with modern CSS
- **Database**: Azure SQL with Stored Procedures
- **AI**: Azure OpenAI (GPT-4o)
- **IaC**: Bicep templates
- **Language**: C# and Python

## 📝 For Contributors

This repo is a template for demonstrating app modernization. To test prompt changes:

1. Fork this repository (rename to avoid confusion, e.g., "AMA-Test01")
2. Replace screenshots and database schema with your test case
3. Update prompt files in the `prompts/` directory
4. Use GitHub Copilot agent with "modernise my app" command
5. Review generated code and iterate on prompts

**Important**: Always fork before running the agent to avoid polluting the base template.

## 🎓 Learning Resources

- [Azure App Service](https://docs.microsoft.com/azure/app-service/)
- [Managed Identity](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [Azure OpenAI](https://docs.microsoft.com/azure/cognitive-services/openai/)
- [Bicep IaC](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)

## 🧹 Cleanup

Remove all Azure resources:
```bash
az group delete --name rg-expensemgmt-demo --yes --no-wait
```

## 📄 License

See [LICENSE](./LICENSE) file for details.

---

**Note**: This is a proof-of-concept for demonstration purposes. For production use, implement additional security measures, authentication, monitoring, and backup strategies as outlined in DEPLOYMENT.md.
