# 12. Local orchestration with .NET Aspire and Docker Compose; deploy to Azure Container Apps with Terraform

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Running the API needs PostgreSQL (and, since [ADR-0010](0010-keycloak-local-identity-provider.md), Keycloak).
Developers want one command, with logs, traces and metrics visible. Not everyone has the Aspire CLI, and a
container-only path is closer to how the image runs in production. The reference also needs a concrete, reviewable
deployment target.

## Decision

Offer two local paths and one cloud path, all configured through the same keys.

- **Aspire (recommended):** [`AppHost.cs`](../../src/CleanArchitecture.AppHost/AppHost.cs) provisions PostgreSQL
  (resource `Database`, injected as `ConnectionStrings__Database`) and Keycloak, and waits for both before starting
  the API. [`ServiceDefaults`](../../src/CleanArchitecture.ServiceDefaults/Extensions.cs) centralises
  OpenTelemetry, service discovery, HTTP resilience, and the `/health` and `/alive` probes; Infrastructure
  deliberately does not configure OpenTelemetry itself.
- **Docker Compose:** [`docker-compose.yml`](../../docker-compose.yml) builds the API from
  [`Dockerfile`](../../src/CleanArchitecture.Presentation/Dockerfile) and runs it with PostgreSQL 17 and Keycloak,
  gated on health checks.
- **Azure:** [`terraform/main.tf`](../../terraform/main.tf) provisions a resource group, Log Analytics, a Container
  Apps environment and app (liveness `/alive`, readiness `/health`), and PostgreSQL Flexible Server. It is a flat,
  single-environment example with no modules or workspaces.
- **Delivery:** [`ci.yml`](../../.github/workflows/ci.yml) builds, tests, checks formatting, builds the image, and
  validates Terraform; [`cd.yml`](../../.github/workflows/cd.yml) reuses CI as a gate and pushes SHA and `latest`
  images to GHCR. `terraform apply` is run by an operator, not by the pipeline.

## Consequences

**Positive**

- One command locally, with a telemetry dashboard under Aspire.
- The connection-string key is identical across Aspire, Compose, Terraform and tests, so no code differs by
  environment.
- Infrastructure is reviewable code and validated on every pull request.

**Negative**

- Two local orchestrators to keep in sync (ports, Keycloak settings, environment variables).
- The Terraform example does not configure `Authentication:Schemes:Bearer`, so a fresh deployment rejects protected
  calls with 401 until an issuer is set; it also uses a single database admin login and no private networking.
- Deployment is manual; there is no environment promotion or automated rollout.
- Tied to Azure for the cloud path; other targets need their own IaC.

## Alternatives considered

- **Docker Compose only.** One path, but no dashboard or integrated telemetry for local debugging.
- **Aspire only.** Less duplication; excludes developers without the Aspire tooling.
- **Kubernetes (AKS) or App Service.** More control, or more PaaS; heavier to operate or less container-native than
  Container Apps for a single API.
- **Bicep or `aspire deploy`.** Azure-native options that avoid Terraform state; the repository does not record why
  Terraform was preferred, but it keeps the same IaC tool usable beyond Azure.
