// Application Insights — observability
// Interview answer: "What is the difference between logs, traces, and metrics?"
// Logs: discrete events — "User 123 logged in at 14:32"
// Traces: the path of one request through your system — request → handler → DB → response
// Metrics: aggregated numbers over time — "500 requests/second, p99 latency 120ms"
// App Insights captures all three. Every HTTP request gets a unique operation ID
// that links all log lines, dependency calls, and exceptions for that request.

param name     string
param location string
param tags     object

// Log Analytics Workspace — the storage backend for App Insights
// Interview answer: "What is a Log Analytics Workspace?"
// It's the database where all your telemetry lives. You query it with KQL
// (Kusto Query Language). Multiple App Insights instances can share one workspace.
resource logWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name     : '${name}-workspace'
  location : location
  tags     : tags
  properties: {
    retentionInDays: 30  // how long logs are kept — increase for prod compliance
    sku: {
      name: 'PerGB2018'  // pay per GB ingested
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name      : name
  location  : location
  tags      : tags
  kind      : 'web'
  properties: {
    Application_Type               : 'web'
    WorkspaceResourceId            : logWorkspace.id
    IngestionMode                  : 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery    : 'Enabled'
  }
}

output name             string = appInsights.name
output connectionString string = appInsights.properties.ConnectionString
output instrumentationKey string = appInsights.properties.InstrumentationKey
