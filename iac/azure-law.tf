resource "azurerm_log_analytics_workspace" "law" {
    name                = "bookvaultlaw${var.env_id}"
    location            = azurerm_resource_group.bookvaultrg.location
    resource_group_name = azurerm_resource_group.bookvaultrg.name
    sku                 = "PerGB2018"
    retention_in_days   = 30
    
    tags = { 
        environment = var.env_id
        src     = var.src_key
    }
  
}