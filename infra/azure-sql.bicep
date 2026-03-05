// azure-sql.bicep
// Deploys: Azure SQL Server (Entra ID only auth) + Northwind database
// Uses stable API @2021-11-01

@description('Location for resources')
param location string = 'uksouth'

@description('Object ID of the Entra ID administrator')
param adminObjectId string

@description('UPN (login) of the Entra ID administrator')
param adminLogin string

@description('Principal ID of the managed identity for role assignment')
param managedIdentityPrincipalId string

@description('Name of the managed identity (used as DB user)')
param managedIdentityName string

var suffix = uniqueString(resourceGroup().id)
var sqlServerName = 'sql-expensemgmt-${suffix}'

// ── SQL Server ───────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: adminLogin
      sid: adminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// ── Azure AD Only Authentication ─────────────────────────────────────────────

resource azureAdOnlyAuth 'Microsoft.Sql/servers/azureADOnlyAuthentications@2021-11-01' = {
  parent: sqlServer
  name: 'Default'
  properties: {
    azureADOnlyAuthentication: true
  }
}

// ── Database ─────────────────────────────────────────────────────────────────

resource database 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: 'Northwind'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

// ── Firewall: Allow Azure Services ───────────────────────────────────────────

resource firewallAzure 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ── Outputs ──────────────────────────────────────────────────────────────────

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
output sqlServerName string = sqlServer.name
