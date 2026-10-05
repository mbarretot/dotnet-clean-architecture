# 12. Local orchestration with .NET Aspire and Docker Compose; deploy to Azure Container Apps with Terraform

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Developers need a one-command local stack and a reviewable cloud target. Aspire supplies telemetry; a container-only path accommodates developers without its CLI.

## Decision

Offer Aspire and Compose locally, and Terraform-managed Azure Container Apps/PostgreSQL in the cloud. Share configuration keys across environments; ServiceDefaults owns telemetry, discovery, resilience and health probes.

## Outcome

Local orchestration waits for dependencies. CI validates code/images/IaC; CD publishes SHA/latest GHCR images, while an operator runs Terraform for deployment. Redis is connected locally and optional in Terraform configuration.

## Consequences

### Benefits

- Aspire supplies a local logs/traces/metrics dashboard.
- Infrastructure changes are reviewable and validated as code.

### Trade-offs

- Two local orchestrators require synchronized configuration.
- The flat Terraform example uses an admin database login/public networking and does not provision identity/Redis or inject JWT settings; protected requests remain 401 until configured.
- Cloud deployment is Azure-specific and manual: no environment promotion or automated rollout.

## Alternatives

- **Compose only:** no integrated telemetry dashboard.
- **Aspire only:** excludes developers without its tools.
- **AKS/App Service:** more operational machinery or less container-native for one API.
- **Bicep/aspire deploy:** viable Azure-native options; history does not establish why Terraform won.

## References

- [AppHost](../../src/CleanArchitecture.AppHost/AppHost.cs)
- [Compose](../../docker-compose.yml)
- [Service defaults](../../src/CleanArchitecture.ServiceDefaults/Extensions.cs)
- [Azure resources](../../terraform/main.tf)
- [Image publication](../../.github/workflows/cd.yml)
- [ADR-0010](0010-keycloak-local-identity-provider.md)
