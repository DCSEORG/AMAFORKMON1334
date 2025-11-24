// Main Bicep template
targetScope = 'resourceGroup'

param location string = resourceGroup().location
param appServiceName string = 'app-expensemgmt-${uniqueString(resourceGroup().id)}'
param appServicePlanName string = 'plan-expensemgmt-${uniqueString(resourceGroup().id)}'
param sqlServerName string = 'sql-expensemgmt-${uniqueString(resourceGroup().id)}'
param databaseName string = 'Northwind'
param adminLogin string
param adminObjectId string
param deployGenAI bool = false
param openAIName string = 'openai-expensemgmt-${uniqueString(resourceGroup().id)}'
param searchServiceName string = 'search-expensemgmt-${uniqueString(resourceGroup().id)}'

// Deploy App Service with Managed Identity
module appServiceModule 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    appServiceName: appServiceName
    appServicePlanName: appServicePlanName
  }
}

// Deploy Azure SQL
module azureSqlModule 'azure-sql.bicep' = {
  name: 'azureSqlDeployment'
  params: {
    location: location
    sqlServerName: sqlServerName
    databaseName: databaseName
    adminLogin: adminLogin
    adminObjectId: adminObjectId
    managedIdentityPrincipalId: appServiceModule.outputs.managedIdentityPrincipalId
  }
}

// Conditionally deploy GenAI resources
module genAIModule 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: location
    openAIName: openAIName
    searchServiceName: searchServiceName
    managedIdentityPrincipalId: appServiceModule.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appServiceModule.outputs.appServiceName
output appServiceUrl string = appServiceModule.outputs.appServiceUrl
output managedIdentityClientId string = appServiceModule.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appServiceModule.outputs.managedIdentityPrincipalId
output managedIdentityName string = appServiceModule.outputs.managedIdentityName
output sqlServerFqdn string = azureSqlModule.outputs.sqlServerFqdn
output databaseName string = azureSqlModule.outputs.databaseName
output openAIEndpoint string = deployGenAI ? genAIModule.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAIModule.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAIModule.outputs.searchEndpoint : ''
