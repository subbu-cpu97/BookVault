// main.bicep — root file that orchestrates all modules
// Interview answer: "What is Bicep and why use it over ARM templates?"
// Bicep is a domain-specific language that compiles to ARM (Azure Resource Manager)
// templates. ARM JSON is verbose and error-prone — Bicep is concise and readable.
// Infrastructure as Code means your entire Azure environment is version-controlled.
// Anyone can destroy and recreate it identically with one command.
// No clicking in the portal — no undocumented manual steps.

targetScope = 'resourceGroup'

// ── Parameters ────────────────────────────────────────────────────
// Parameters make the template reusable across environments (dev, staging, prod)
@description('Environment name — used to name all resources')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('PostgreSQL admin password — passed from Key Vault or pipeline secret')
@secure()
param postgresAdminPassword string

@description('JWT secret key — min 32 characters')
@secure()
param jwtSecretKey string

// ── Variables ─────────────────────────────────────────────────────
// Naming convention: bookvault-{resource}-{environment}
// Consistent naming makes resources easy to find in the portal
var prefix       = 'bookvault'
var envSuffix    = environment
var resourceTags = {
  Project     : 'BookVault'
  Environment : environment
  ManagedBy   : 'Bicep'
}

// ── Modules ───────────────────────────────────────────────────────
// Each module is a separate .bicep file — separation of concerns
// Interview answer: "Why split Bicep into modules?"
// One 500-line file is hard to review and debug.
// Modules are focused: containerRegistry.bicep only creates ACR.
// Teams can own different modules. Changes to one don't affect others.

module containerRegistry 'modules/containerRegistry.bicep' = {
  name: 'containerRegistry'
  params: {
    name     : '${prefix}acr${envSuffix}'
    location : location
    tags     : resourceTags
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    serverName    : '${prefix}-postgres-${envSuffix}'
    location      : location
    adminPassword : postgresAdminPassword
    tags          : resourceTags
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    name                : '${prefix}-kv-${envSuffix}'
    location            : location
    postgresConnString  : postgres.outputs.connectionString
    jwtSecretKey        : jwtSecretKey
    tags                : resourceTags
  }
}

module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsights'
  params: {
    name     : '${prefix}-insights-${envSuffix}'
    location : location
    tags     : resourceTags
  }
}

module containerApp 'modules/containerApp.bicep' = {
  name: 'containerApp'
  params: {
    name                   : '${prefix}-api-${envSuffix}'
    location               : location
    containerRegistryName  : containerRegistry.outputs.name
    containerRegistryServer: containerRegistry.outputs.loginServer
    keyVaultName           : keyVault.outputs.name
    appInsightsConnString  : appInsights.outputs.connectionString
    tags                   : resourceTags
  }
}

// ── Outputs ───────────────────────────────────────────────────────
// Outputs are referenced by the CD pipeline
output containerAppUrl        string = containerApp.outputs.url
output containerRegistryLogin string = containerRegistry.outputs.loginServer
output keyVaultName           string = keyVault.outputs.name
