resource "azurerm_resource_group" "bookvaultrg" {
  name     = "bookvault-rg"
  location = "UK South"

  tags = { 
    environment = var.env_id
    src     = "terraform"
  }
}