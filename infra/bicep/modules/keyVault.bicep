// Azure Key Vault — secrets management
// Interview answer: "Why Key Vault instead of environment variables?"
// Environment variables are visible in the Azure portal to anyone with
// portal access. They're also logged in deployment pipelines.
// Key Vault secrets are encrypted at rest, access-controlled by Azure RBAC,
// audit-logged (every read is recorded), and never appear in plain text.
// The pattern: app authenticates to Key Vault with Managed Identity
// (no password), reads the secret, uses it. Zero secrets in config files.

param name               string
param location           string
@secure()
param postgresConnString string
@secure()
param jwtSecretKey       string
param tags               object

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name     : name
  location : location
  tags     : tags
  properties: {
    sku: {
      family: 'A'
      name  : 'standard'
    }
    tenantId                : tenant().tenantId
    enableRbacAuthorization : true   // use Azure RBAC, not access policies
    // Soft delete: secrets recoverable for 7 days after deletion
    // Interview answer: "What is soft delete in Key Vault?"
    // Without soft delete, deleting a secret is permanent and instant.
    // With soft delete, you have a recovery window — critical for accidents.
    enableSoftDelete        : true
    softDeleteRetentionInDays: 7
    enablePurgeProtection   : false  // set true for prod
  }
}

// Store secrets — names use hyphens (Key Vault naming rule)
resource secretPostgres 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent : kv
  name   : 'postgres-connection-string'
  properties: {
    value: postgresConnString
  }
}

resource secretJwt 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent : kv
  name   : 'jwt-secret-key'
  properties: {
    value: jwtSecretKey
  }
}

output name      string = kv.name
output vaultUri  string = kv.properties.vaultUri
output resourceId string = kv.id
