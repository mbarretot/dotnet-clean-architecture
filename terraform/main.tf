# Single-environment example deployment: Azure Container Apps running the Presentation image,
# backed by Azure Database for PostgreSQL Flexible Server. No workspaces, no nested modules —
# kept flat and readable on purpose since this is a reference project, not a production estate.

locals {
  resource_prefix = "${var.project_name}-${var.environment_name}"

  # Azure Container Apps env vars use the same double-underscore convention as .NET configuration,
  # so this must stay "ConnectionStrings__Database" to match Infrastructure/DependencyInjection.cs
  # (configuration key "ConnectionStrings:Database") without any code changes.
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
    # Matches Presentation's ASPNETCORE_HTTP_PORTS=8080 from the Dockerfile.
    transport = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  depends_on = [azurerm_postgresql_flexible_server_firewall_rule.allow_azure_services]
}

resource "azurerm_postgresql_flexible_server" "this" {
  name                   = "psql-${local.resource_prefix}"
  resource_group_name    = azurerm_resource_group.this.name
  location               = azurerm_resource_group.this.location
  version                = var.postgres_version
  administrator_login    = var.postgres_admin_login
  administrator_password = var.postgres_admin_password
  storage_mb             = var.postgres_storage_mb
  sku_name               = var.postgres_sku_name

  # Single-zone, no HA/replicas: appropriate for an example deployment, not production.
  zone = "1"

  tags = var.tags
}

resource "azurerm_postgresql_flexible_server_database" "this" {
  name      = var.postgres_database_name
  server_id = azurerm_postgresql_flexible_server.this.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

# The special 0.0.0.0/0.0.0.0 range is Azure's documented way to allow access from other Azure
# services (including this Container App) without opening the server to the public internet at large.
resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_postgresql_flexible_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
