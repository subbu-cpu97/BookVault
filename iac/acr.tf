resource "azurerm_container_registry" "bookvaultacr" {
  location            = azurerm_resource_group.bookvaultrg.location
  name                = "bookvaultacr${var.env_id}"
  resource_group_name = azurerm_resource_group.bookvaultrg.name
  sku                 = "Standard"
  admin_enabled       = true
  public_network_access_enabled = true


  tags = { 
    environment = var.env_id
    src     = var.src_key
  }

}