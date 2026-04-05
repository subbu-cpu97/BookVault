// Azure Container Registry — private Docker image store
// Interview answer: "Why ACR instead of Docker Hub?"
// Docker Hub is public by default and rate-limited.
// ACR is private, lives in your Azure subscription, same region as your app
// so image pulls are fast and free. Integrated with Azure RBAC —
// your Container App pulls images using Managed Identity, no password.

param name     string
param location string
param tags     object

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name     : name
  location : location
  tags     : tags
  sku: {
    // Basic: sufficient for dev/staging. Standard adds geo-replication.
    // Premium adds private endpoints. Match SKU to your scale needs.
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false  // use Managed Identity, not admin credentials
  }
}

output name        string = acr.name
output loginServer string = acr.properties.loginServer
output resourceId  string = acr.id
