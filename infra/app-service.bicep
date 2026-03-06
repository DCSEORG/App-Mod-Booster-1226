// app-service.bicep
// Deploys: User-Assigned Managed Identity, App Service Plan, App Service
// Region: UK South

@description('Location for resources')
param location string = 'uksouth'

@description('Unique suffix derived from resource group')
var suffix = uniqueString(resourceGroup().id)

// ── Managed Identity ────────────────────────────────────────────────────────

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mid-appmodassist-${suffix}'
  location: location
}

// ── App Service Plan (Standard S1) ─────────────────────────────────────────

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'asp-expensemgmt-${suffix}'
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
  properties: {
    reserved: false
  }
}

// ── App Service ─────────────────────────────────────────────────────────────

resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: 'app-expensemgmt-${suffix}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentity.properties.clientId
        }
      ]
    }
  }
}

// ── Outputs ─────────────────────────────────────────────────────────────────

output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output managedIdentityResourceId string = managedIdentity.id
output managedIdentityName string = managedIdentity.name
