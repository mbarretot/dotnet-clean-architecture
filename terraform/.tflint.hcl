config {
  call_module_type = "local"
}

plugin "terraform" {
  enabled = true
  preset  = "recommended"
}

plugin "azurerm" {
  enabled = true
  version = "0.32.0"
  source  = "github.com/terraform-linters/tflint-ruleset-azurerm"
}

# Environments built from this reference are disposable (`terraform destroy` per environment), and
# prevent_destroy cannot be made conditional. Production stacks should enable it on the PostgreSQL resources.
rule "azurerm_resources_missing_prevent_destroy" {
  enabled = false
}
