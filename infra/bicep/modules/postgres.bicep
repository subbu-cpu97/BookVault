// Azure Database for PostgreSQL Flexible Server
// Interview answer: "What is Flexible Server vs Single Server?"
// Single Server is the old model — being retired by Microsoft.
// Flexible Server is the current model: you can stop/start it (saving cost),
// configure maintenance windows, and choose between zone-redundant HA
// or no HA. For dev use Burstable SKU (cheapest). For prod use
// GeneralPurpose with zone-redundant standby.

param serverName    string
param location      string
@secure()
param adminPassword string
param tags          object

var adminUser   = 'bookvault_admin'
var databaseName = 'bookvault_catalog'

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name     : serverName
  location : location
  tags     : tags
  sku: {
    name: 'Standard_B1ms'  // Burstable — cheapest, fine for dev
    tier: 'Burstable'
  }
  properties: {
    version               : '16'
    administratorLogin    : adminUser
    administratorLoginPassword: adminPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays : 7
      geoRedundantBackup  : 'Disabled'  // enable for prod
    }
    // Interview answer: "What is high availability in Azure Postgres?"
    // Zone-redundant HA keeps a standby replica in a different availability zone.
    // If the primary fails, Azure promotes the standby in ~60 seconds.
    // For prod: set mode to 'ZoneRedundant'. For dev: 'Disabled' saves cost.
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgresServer
  name  : databaseName
}

// Allow Azure services to connect (Container Apps is an Azure service)
resource firewallRuleAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent   : postgresServer
  name     : 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress  : '0.0.0.0'
  }
}

output serverName      string = postgresServer.name
output databaseName    string = database.name
output connectionString string = 'Host=${postgresServer.properties.fullyQualifiedDomainName};Port=5432;Database=${databaseName};Username=${adminUser};Password=${adminPassword};SSL Mode=Require'
