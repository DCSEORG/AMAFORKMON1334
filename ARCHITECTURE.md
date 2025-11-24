# Azure Expense Management System - Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         AZURE CLOUD PLATFORM                         │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                    Resource Group (UKSOUTH)                     │ │
│  │                                                                  │ │
│  │  ┌──────────────────────┐       ┌──────────────────────────┐  │ │
│  │  │   App Service (S1)   │       │  User-Assigned Managed   │  │ │
│  │  │  ASP.NET Core 8.0    │◄──────│      Identity            │  │ │
│  │  │  - Razor Pages UI    │       │                          │  │ │
│  │  │  - REST APIs         │       └──────────┬───────────────┘  │ │
│  │  │  - Swagger Docs      │                  │                   │ │
│  │  └────────┬─────────────┘                  │                   │ │
│  │           │                                 │                   │ │
│  │           │ Managed Identity Auth           │                   │ │
│  │           │                                 │                   │ │
│  │  ┌────────▼─────────────┐       ┌──────────▼───────────────┐  │ │
│  │  │  Azure SQL Database  │◄──────│  Entra ID (Azure AD)     │  │ │
│  │  │  - Northwind DB      │       │  - Admin User            │  │ │
│  │  │  - Basic Tier        │       │  - AD-Only Auth          │  │ │
│  │  │  - Stored Procedures │       │                          │  │ │
│  │  └──────────────────────┘       └──────────────────────────┘  │ │
│  │                                                                  │ │
│  │  Optional GenAI Services (deploy-with-chat.sh):                │ │
│  │                                                                  │ │
│  │  ┌──────────────────────┐       ┌──────────────────────────┐  │ │
│  │  │ Azure OpenAI (GPT-4o)│       │  Azure Cognitive Search  │  │ │
│  │  │  - Sweden Central    │       │  - RAG Pattern           │  │ │
│  │  │  - S0 Tier           │       │  - Basic Tier            │  │ │
│  │  │  - Managed Identity  │       │                          │  │ │
│  │  └──────────────────────┘       └──────────────────────────┘  │ │
│  │           ▲                               ▲                     │ │
│  │           │                               │                     │ │
│  │           └───────────┬───────────────────┘                     │ │
│  │                       │                                         │ │
│  │              ┌────────▼─────────────┐                          │ │
│  │              │   Chat UI & APIs     │                          │ │
│  │              │  Function Calling    │                          │ │
│  │              └──────────────────────┘                          │ │
│  │                                                                  │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘

                              ▲
                              │
                              │ HTTPS
                              │
                    ┌─────────┴──────────┐
                    │   End Users        │
                    │  - Add Expenses    │
                    │  - View Expenses   │
                    │  - Approve Items   │
                    │  - AI Chat         │
                    └────────────────────┘
```

## Component Descriptions

### App Service
- **Location**: UK South
- **SKU**: S1 Standard (avoids cold starts)
- **Runtime**: .NET 8.0 (LTS) on Linux
- **Features**: 
  - Modern Razor Pages UI
  - REST APIs with Swagger documentation
  - Error handling with dummy data fallback
  - Managed Identity authentication

### User-Assigned Managed Identity
- **Purpose**: Secure authentication to Azure resources
- **Access**: 
  - Azure SQL Database (db_datareader, db_datawriter roles)
  - Azure OpenAI (Cognitive Services OpenAI User role)
  - Azure Cognitive Search (Search Index Data Contributor role)

### Azure SQL Database
- **Database**: Northwind
- **Tier**: Basic (development)
- **Authentication**: Entra ID (Azure AD) Only
- **Schema**: Expense Management System
  - Users, Roles, Expenses, Categories, Status
  - Stored procedures for all operations

### Azure OpenAI (Optional)
- **Location**: Sweden Central (GPT-4o availability)
- **Model**: GPT-4o
- **SKU**: S0
- **Purpose**: AI-powered chat assistant with function calling

### Azure Cognitive Search (Optional)
- **Tier**: Basic
- **Purpose**: RAG pattern for contextual AI responses

## Deployment Options

### Option 1: Basic Deployment (deploy.sh)
- Deploys App Service, SQL Database, and web application
- No GenAI services
- Chat UI shows placeholder message

### Option 2: Full Deployment (deploy-with-chat.sh)
- Deploys everything from Option 1
- Adds Azure OpenAI and Cognitive Search
- Enables full AI chat functionality with function calling

## Security Features

1. **Entra ID Only Authentication** - No SQL passwords stored
2. **Managed Identity** - No credential management needed
3. **HTTPS Only** - All traffic encrypted
4. **Role-Based Access** - Proper database permissions
5. **Firewall Rules** - Controlled access to SQL Server
