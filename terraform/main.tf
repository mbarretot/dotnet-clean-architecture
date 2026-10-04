locals {
  resource_prefix = "${var.project_name}-${var.environment_name}"

  database_connection_string = "Host=${azurerm_postgresql_flexible_server.this.fqdn};Port=5432;Database=${var.postgres_database_name};Username=${var.postgres_admin_login};Password=${var.postgres_admin_password};Ssl Mode=Require;Trust Server Certificate=true"
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${local.resource_prefix}"
  location = var.location
  tags     = var.tags
}

resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-${local.resource_prefix}"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_container_app_environment" "this" {
  name                       = "cae-${local.resource_prefix}"
  resource_group_name        = azurerm_resource_group.this.name
  location                   = azurerm_resource_group.this.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id
  tags                       = var.tags
}

resource "azurerm_container_app" "api" {
  name                         = "ca-${local.resource_prefix}-api"
  resource_group_name          = azurerm_resource_group.this.name
  container_app_environment_id = azurerm_container_app_environment.this.id
  revision_mode                = "Single"
  tags                         = var.tags

  secret {
    name  = "database-connection-string"
    value = local.database_connection_string
  }

  dynamic "secret" {
    for_each = var.cache_connection_string == null ? [] : [var.cache_connection_string]

    content {
      name  = "cache-connection-string"
      value = secret.value
    }
  }

  template {
    container {
      name   = "api"
      image  = var.container_image
      cpu    = var.container_cpu
      memory = var.container_memory

      env {
        name        = "ConnectionStrings__Database"
        secret_name = "database-connection-string"
      }

      dynamic "env" {
        for_each = var.cache_connection_string == null ? [] : ["cache-connection-string"]

        content {
          name        = "ConnectionStrings__Cache"
          secret_name = env.value
        }
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      liveness_probe {
        transport = "HTTP"
        path      = "/alive"
        port      = 8080
      }

      readiness_probe {
        transport = "HTTP"
        path      = "/health"
        port      = 8080
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  depends_on = [azurerm_postgresql_flexible_server_firewall_rule.allow_azure_services]
}

resource "azurerm_postgresql_flexible_server" "this" {
  #checkov:skip=CKV_AZURE_136:Dev-sized burstable server; geo-redundant backup is a per-environment cost decision.
  #checkov:skip=CKV2_AZURE_57:Public access with a firewall keeps the reference deployment VNet-free; see ADR-0012.
  name                   = "psql-${local.resource_prefix}"
  resource_group_name    = azurerm_resource_group.this.name
  location               = azurerm_resource_group.this.location
  version                = var.postgres_version
  administrator_login    = var.postgres_admin_login
  administrator_password = var.postgres_admin_password
  storage_mb             = var.postgres_storage_mb
  sku_name               = var.postgres_sku_name

  zone = "1"

  tags = var.tags
}

resource "azurerm_postgresql_flexible_server_database" "this" {
  name      = var.postgres_database_name
  server_id = azurerm_postgresql_flexible_server.this.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure_services" {
  #checkov:skip=CKV2_AZURE_26:0.0.0.0 is Azure's "allow Azure services" rule the Container App needs without a VNet.
  name             = "AllowAzureServices"
  server_id        = azurerm_postgresql_flexible_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
