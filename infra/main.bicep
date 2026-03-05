// main.bicep
// Orchestrates: app-service, azure-sql, and (optionally) genai modules

@description('Location for all resources')
param location string = 'uksouth'

@description('Object ID of the Entra ID administrator for SQL Server')
param adminObjectId string

@description('UPN/login of the Entra ID administrator for SQL Server')
param adminLogin string

@description('Whether to deploy GenAI resources (Azure OpenAI + AI Search)')
param deployGenAI bool = false

// ── App Service Module ────────────────────────────────────────────────────────

module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
  }
}

// ── Azure SQL Module ──────────────────────────────────────────────────────────

module azureSql 'azure-sql.bicep' = {
  name: 'azureSqlDeployment'
  params: {
    location: location
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    managedIdentityName: appService.outputs.managedIdentityName
  }
}

// ── GenAI Module (conditional) ───────────────────────────────────────────────

module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genaiDeployment'
  params: {
    location: location
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// ── Outputs ──────────────────────────────────────────────────────────────────

output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output managedIdentityResourceId string = appService.outputs.managedIdentityResourceId
output managedIdentityName string = appService.outputs.managedIdentityName
output sqlServerFqdn string = azureSql.outputs.sqlServerFqdn
output databaseName string = azureSql.outputs.databaseName
output sqlServerName string = azureSql.outputs.sqlServerName
output openAIEndpoint string = deployGenAI ? genai.outputs!.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs!.openAIModelName : ''
output openAIName string = deployGenAI ? genai.outputs!.openAIName : ''
output searchEndpoint string = deployGenAI ? genai.outputs!.searchEndpoint : ''
