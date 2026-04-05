resource "azurerm_postgresql_flexible_server" "postgres" {
    name                = "bookvaultpg${var.env_id}"
    resource_group_name = azurerm_resource_group.bookvaultrg.name
    location            = azurerm_resource_group.bookvaultrg.location

    administrator_login          = "pgadmin"
    administrator_password = var.pg-sql-pwd


    sku_name = "GP_Standard_D2s_v3"
    version    = "14"
    storage_mb = 32768

    backup_retention_days = 7

    zone = "1"

    authentication {
        password_auth_enabled = true
    }

    tags = { 
        environment = var.env_id
        src     = var.src_key
    }
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_my_ip" {
  name             = "AllowMyIP"
  server_id        = azurerm_postgresql_flexible_server.postgres.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"

}


resource "azurerm_postgresql_flexible_server_database" "db" {
  name      = "bookvaultdb"
  server_id = azurerm_postgresql_flexible_server.postgres.id
  collation = "en_US.utf8"
  charset   = "UTF8"

}