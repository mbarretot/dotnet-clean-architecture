output "container_app_fqdn" {
  description = "Public FQDN of the Presentation API Container App."
  value       = azurerm_container_app.api.latest_revision_fqdn
}

output "container_app_url" {
  description = "Public HTTPS URL of the Presentation API."
  value       = "https://${azurerm_container_app.api.latest_revision_fqdn}"
}

output "postgres_fqdn" {
  description = "Fully-qualified domain name of the PostgreSQL Flexible Server."
  value       = azurerm_postgresql_flexible_server.this.fqdn
}

output "resource_group_name" {
  description = "Name of the resource group holding every resource in this deployment."
  value       = azurerm_resource_group.this.name
}
