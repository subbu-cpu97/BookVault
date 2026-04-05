resource "azurerm_container_app_environment" "app_env" {
    name                = "bookvaultappenv${var.env_id}"
    location            = azurerm_resource_group.bookvaultrg.location
    resource_group_name = azurerm_resource_group.bookvaultrg.name
    log_analytics_workspace_id = azurerm_log_analytics_workspace.law.id
    
    tags = { 
        environment = var.env_id
        src     = var.src_key
    }
  
}