// genai.bicep
// Deploys: Azure OpenAI (swedencentral), AI Search (uksouth)
// Uses the managed identity from app-service.bicep (no new identity created here)

@description('Location for AI Search resource')
param location string = 'uksouth'

@description('Principal ID of the managed identity (from app-service module output)')
param managedIdentityPrincipalId string

var suffix = uniqueString(resourceGroup().id)

// Azure OpenAI MUST be lowercase and in swedencentral
var openAIName = 'oai-expensemgmt-${suffix}'
var searchName = 'srch-expensemgmt-${suffix}'

// ── Azure OpenAI (Sweden Central) ────────────────────────────────────────────

resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIName
  location: 'swedencentral'
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: 'Enabled'
  }
}

// ── GPT-4o Deployment ────────────────────────────────────────────────────────

resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
  }
}

// ── AI Search (Cognitive Search) ─────────────────────────────────────────────

resource aiSearch 'Microsoft.Search/searchServices@2022-09-01' = {
  name: searchName
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    publicNetworkAccess: 'enabled'
  }
}

// ── Role: Cognitive Services OpenAI User → Managed Identity ──────────────────
// Role definition ID for "Cognitive Services OpenAI User": 5e0bd9bd-7b93-4f28-af87-19fc36ad61bd

resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ── Role: Search Index Data Reader → Managed Identity ────────────────────────
// Role definition ID for "Search Index Data Reader": 1407120a-92aa-4202-b7e9-c0e197c71c8f

resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiSearch.id, managedIdentityPrincipalId, '1407120a-92aa-4202-b7e9-c0e197c71c8f')
  scope: aiSearch
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ── Outputs ──────────────────────────────────────────────────────────────────

output openAIEndpoint string = openAI.properties.endpoint
output openAIModelName string = gpt4oDeployment.name
output openAIName string = openAI.name
output searchEndpoint string = 'https://${aiSearch.name}.search.windows.net'
