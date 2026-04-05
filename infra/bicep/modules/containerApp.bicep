// Azure Container Apps — serverless container hosting
// Interview answer: "What is Azure Container Apps?"
// Container Apps is managed Kubernetes. Microsoft runs the K8s control plane.
// You deploy containers and define scaling rules — Azure handles everything else.
// It scales to zero (no instances = no cost) and scales out automatically
// under load. Each deployment creates a new "revision" — you can split traffic
// between revisions for canary deployments or instant rollback.

param name                    string
param location                string
param containerRegistryName   string
param containerRegistryServer string
param keyVaultName            string
param appInsightsConnString   string
param tags                    object

// Container Apps Environment — the shared network/logging boundary
// Multiple Container Apps share one environment (like a K8s namespace)
resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name     : '${name}-env'
  location : location
  tags     : tags
  properties: {
    // App logs sent to Log Analytics automatically
    appLogsConfiguration: {
      destination: 'azure-monitor'
    }
  }
}

// User-assigned Managed Identity for the Container App
// Interview answer: "System-assigned vs user-assigned Managed Identity?"
// System-assigned: tied to the resource lifecycle — deleted when app is deleted.
// User-assigned: independent lifecycle — can be assigned to multiple resources.
// We use user-assigned so we can pre-grant Key Vault access before the app deploys.
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name     : '${name}-identity'
  location : location
  tags     : tags
}

// Grant identity permission to pull images from ACR
// AcrPull role = read images from the registry, nothing else
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name  : guid(acr.id, identity.id, 'acrpull')
  scope : acr
  properties: {
    principalId      : identity.properties.principalId
    // AcrPull built-in role ID — never changes across Azure subscriptions
    roleDefinitionId : subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

// Grant identity permission to read secrets from Key Vault
// Key Vault Secrets User = read secret values, nothing else
resource kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource kvSecretUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name  : guid(kv.id, identity.id, 'kvsecretuser')
  scope : kv
  properties: {
    principalId      : identity.properties.principalId
    // Key Vault Secrets User built-in role ID
    roleDefinitionId : subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: 'ServicePrincipal'
  }
}

// The Container App itself
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name     : name
  location : location
  tags     : tags
  identity: {
    type                  : 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    environmentId: environment.id
    configuration: {
      // Ingress: expose the app to the internet on HTTPS
      ingress: {
        external  : true
        targetPort: 8080
        transport : 'http'
        // Traffic rules: 100% to latest revision
        // Change to split traffic for canary deployments
        traffic: [{
          latestRevision: true
          weight        : 100
        }]
      }
      // Pull images from ACR using the managed identity
      registries: [{
        server   : containerRegistryServer
        identity : identity.id
      }]
      // Secrets referenced from Key Vault via managed identity
      // Interview answer: "How does Container Apps read Key Vault secrets?"
      // You declare secretRef entries that point to Key Vault secret URIs.
      // Container Apps fetches the secret at startup using the managed identity.
      // The secret value is injected as an environment variable.
      // Rotating the secret in Key Vault + restarting the app picks up the new value.
      secrets: [
        {
          name        : 'postgres-conn-string'
          keyVaultUrl : 'https://${keyVaultName}.vault.azure.net/secrets/postgres-connection-string'
          identity    : identity.id
        }
        {
          name        : 'jwt-secret-key'
          keyVaultUrl : 'https://${keyVaultName}.vault.azure.net/secrets/jwt-secret-key'
          identity    : identity.id
        }
      ]
    }
    template: {
      containers: [{
        name : 'bookvault-api'
        // Image updated by CD pipeline — placeholder here
        image: '${containerRegistryServer}/bookvault-api:latest'
        resources: {
          cpu   : '0.5'   // 0.5 vCPU
          memory: '1Gi'
        }
        env: [
          {
            name : 'ASPNETCORE_ENVIRONMENT'
            value: 'Production'
          }
          {
            name      : 'ConnectionStrings__DefaultConnection'
            secretRef : 'postgres-conn-string'
          }
          {
            name      : 'JwtSettings__SecretKey'
            secretRef : 'jwt-secret-key'
          }
          {
            name : 'JwtSettings__Issuer'
            value: 'BookVault'
          }
          {
            name : 'JwtSettings__Audience'
            value: 'BookVault.Client'
          }
          {
            name : 'JwtSettings__AccessTokenExpiryMinutes'
            value: '15'
          }
          {
            name : 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: appInsightsConnString
          }
        ]
        // Health probe — Container Apps calls this to check the app is alive
        // Interview answer: "What is a liveness probe?"
        // Container Apps calls /health every 30s. If it fails 3 times,
        // the container is restarted. This auto-recovers from hangs.
        probes: [
          {
            type       : 'Liveness'
            httpGet    : {
              path: '/health'
              port: 8080
            }
            periodSeconds    : 30
            failureThreshold : 3
          }
          {
            type    : 'Readiness'
            httpGet : {
              path: '/health'
              port: 8080
            }
            periodSeconds        : 10
            initialDelaySeconds  : 10
          }
        ]
      }]
      // Scaling rules
      // Interview answer: "How does Container Apps auto-scale?"
      // minReplicas 0 = scales to zero when no traffic (saves cost in dev).
      // maxReplicas 10 = ceiling during traffic spikes.
      // The HTTP rule scales one replica per 100 concurrent requests.
      scale: {
        minReplicas: 0
        maxReplicas: 10
        rules: [{
          name: 'http-scaling'
          http: {
            metadata: {
              concurrentRequests: '100'
            }
          }
        }]
      }
    }
  }
  dependsOn: [acrPullRole, kvSecretUserRole]
}

output url        string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output name       string = containerApp.name
output identityId string = identity.id
