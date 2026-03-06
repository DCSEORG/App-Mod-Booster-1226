# Azure Services Architecture Diagram

## Expense Management System - Azure Resources

```
┌─────────────────────────────────────────────────────────────────────┐
│                         User / Browser                              │
└────────────────────────────┬────────────────────────────────────────┘
                             │  HTTPS
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Azure App Service (UK South)                     │
│            ASP.NET Core 8.0 - Razor Pages + REST API                │
│                  Swagger UI  /swagger                               │
│                  Chat UI     /chat                                  │
│                  Dashboard   /Index                                 │
└──────────────┬──────────────────────────┬───────────────────────────┘
               │                          │
               │ Managed Identity Auth    │ Managed Identity Auth
               ▼                          ▼
┌──────────────────────────┐  ┌──────────────────────────────────────┐
│  User-Assigned Managed   │  │         (Optional - GenAI)           │
│       Identity           │  │                                      │
│  mid-appmodassist-xxxxx  │  │  ┌─────────────────────────────────┐ │
└──────────────┬───────────┘  │  │    Azure OpenAI (Sweden Central) │ │
               │              │  │    Model: GPT-4o (capacity 8)    │ │
               │              │  └──────────────┬──────────────────┘ │
               ▼              │                 │                     │
┌──────────────────────────┐  │  ┌──────────────▼──────────────────┐ │
│  Azure SQL Database      │  │  │    Azure AI Search              │ │
│  (UK South)              │  │  │    (UK South, Basic SKU)        │ │
│  Server: sql-xxxxx       │  │  │    RAG / Knowledge Base         │ │
│  Database: Northwind     │  │  └─────────────────────────────────┘ │
│  Auth: Entra ID Only     │  └──────────────────────────────────────┘
│  SKU: Basic (dev)        │
└──────────────────────────┘
```

## Component Details

| Component | Resource Type | SKU | Location |
|-----------|--------------|-----|----------|
| App Service Plan | Microsoft.Web/serverfarms | Standard S1 | UK South |
| App Service | Microsoft.Web/sites | — | UK South |
| Managed Identity | Microsoft.ManagedIdentity/userAssignedIdentities | — | UK South |
| Azure SQL Server | Microsoft.Sql/servers | — | UK South |
| Azure SQL Database | Microsoft.Sql/servers/databases | Basic | UK South |
| Azure OpenAI | Microsoft.CognitiveServices/accounts | S0 | Sweden Central |
| AI Search | Microsoft.Search/searchServices | Basic | UK South |

## Authentication Flow

```
App Service
    │
    │  Uses User-Assigned Managed Identity
    │  (AZURE_CLIENT_ID env var set to MI client ID)
    │
    ├──► Azure SQL Database
    │       Authentication: Active Directory Managed Identity
    │       Connection string uses MI client ID as User Id
    │
    └──► Azure OpenAI  (when GenAI deployed)
            Authentication: ManagedIdentityCredential
            Role: Cognitive Services OpenAI User
```

## Deployment Options

### Without GenAI (default)
```bash
bash deploy.sh
```
- Deploys: App Service + SQL Database
- Chat UI available but returns informational message

### With GenAI
```bash
bash deploy-with-chat.sh
```
- Deploys: App Service + SQL Database + Azure OpenAI + AI Search
- Full AI chat experience with function calling

## URL Structure

```
https://<app-service-name>.azurewebsites.net/
    /Index          → Dashboard
    /Expenses       → Expense list & filters
    /Expenses/Create → New expense form
    /Expenses/{id}  → Expense details, approve/reject
    /Manage/Users   → User management
    /chat           → AI Chat assistant
    /swagger        → API documentation & testing
    /api/expenses   → REST API
    /api/users      → REST API
    /api/categories → REST API
    /api/statuses   → REST API
    /api/chat       → Chat API
```
