resource "azurerm_container_app" "app" {
    name                = "bookvaultapp${var.env_id}"
    resource_group_name = azurerm_resource_group.bookvaultrg.name
    container_app_environment_id = azurerm_container_app_environment.app_env.id
    revision_mode = "Multiple"

    template {
       min_replicas = 1
       max_replicas = 3

       container {
        cpu = 0.25
        memory = "0.5Gi"
        image = "mcr.microsoft.com/k8se/quickstarts:latest"
        name = "bookvault-api${var.env_id}"
    }       
    }
    

  ingress {
    allow_insecure_connections = false
    external_enabled = true
    target_port = 8080

    traffic_weight {
        percentage = 100
        label = "primary"
        latest_revision = true
    }
}
    tags = { 
        environment = var.env_id
        src     = var.src_key
    }
}