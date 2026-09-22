variable "project_name" {
  description = "Short name used as a prefix for every resource name. Keep it lowercase/alphanumeric."
  type        = string
  default     = "cleanarch"
}

variable "environment_name" {
  description = "Deployment environment name (e.g. dev, staging, prod). Used in resource naming and tags."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region to deploy into."
  type        = string
  default     = "westeurope"
}

variable "container_image" {
  description = <<-EOT
    Fully-qualified container image reference for the Presentation API, e.g.
    "myregistry.azurecr.io/cleanarchitecture-api:1.0.0". Built from
    src/CleanArchitecture.Presentation/Dockerfile and pushed to a registry the
    Container App Environment can pull from before running `terraform apply`.
  EOT
  type        = string
}

variable "container_cpu" {
  description = "vCPU cores allocated to the Container App (Consumption plan increments)."
  type        = number
  default     = 0.5
}

variable "container_memory" {
  description = "Memory allocated to the Container App (must pair with container_cpu per Azure's supported combinations)."
  type        = string
  default     = "1Gi"
}

variable "postgres_admin_login" {
  description = "Administrator login for the PostgreSQL Flexible Server."
  type        = string
  default     = "psqladmin"
}

variable "postgres_admin_password" {
  description = "Administrator password for the PostgreSQL Flexible Server. Pass via TF_VAR_postgres_admin_password or a *.auto.tfvars file that is never committed."
  type        = string
  sensitive   = true
}

variable "postgres_sku_name" {
  description = "PostgreSQL Flexible Server SKU (tier_family_cores, e.g. B_Standard_B1ms for burstable dev workloads)."
  type        = string
  default     = "B_Standard_B1ms"
}

variable "postgres_version" {
  description = "PostgreSQL major version."
  type        = string
  default     = "16"
}

variable "postgres_storage_mb" {
  description = "Storage allocated to the PostgreSQL Flexible Server, in MB."
  type        = number
  default     = 32768
}

variable "postgres_database_name" {
  description = "Name of the application database created on the PostgreSQL Flexible Server. Matches the resource name used by the Aspire AppHost (\"Database\") for consistency across environments."
  type        = string
  default     = "cleanarchitecture"
}

variable "tags" {
  description = "Common tags applied to every resource."
  type        = map(string)
  default = {
    project = "clean-architecture-example"
  }
}
