<div align="center">

<img src="docs/assets/clean-architecture-logo.png" alt="Clean Architecture logo" width="150" />

# .NET Clean Architecture

**A working catalog, inventory and order API with explicit business rules and testable boundaries.**

[![CI](https://github.com/mbarretot/dotnet-clean-architecture/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/mbarretot/dotnet-clean-architecture/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-64748b)](LICENSE)

[Use cases](#-use-cases) · [Run](#-run-locally) · [Libraries](#-technology-and-libraries) · [Architecture](#-architecture) · [Guides](#-go-deeper)

</div>

An API-first reference to study and adapt, not a project generator, storefront or complete commerce platform. Follow a business operation from HTTP through application handlers and domain rules to PostgreSQL.

## 🎯 Use cases

| Capability | Current behavior |
|---|---|
| **Catalog** | Search, filter, sort and paginate products; retrieve cached details. |
| **Inventory** | Create/update products, set stock and soft-delete; invalidate cached reads. |
| **Ordering** | Merge duplicate lines, snapshot names/prices, require one currency and reserve stock atomically. |
| **Customer actions** | Read your orders, pay or cancel before shipment; cancellation releases stock for existing products. |
| **Fulfillment** | Ship paid orders, then complete shipped orders. |

Reads require authentication. Writes require `products:write` or `orders:write`; fulfillment requires `orders:fulfill`. Customer handlers enforce ownership; invalid transitions and insufficient stock return `409`.

![Order lifecycle, reservation and cancellation rules](docs/assets/order-lifecycle.svg)

**Scope:** payment and shipment are state changes, not external integrations. No refunds, carrier, cart or storefront. Domain events run in process after saving; there is no durable outbox or broker.

## 🚀 Run locally

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ([pinned version](global.json)), Docker, and [Aspire CLI](https://aspire.dev/get-started/install-cli/) for the recommended path.

```bash
aspire run
```

Starts API, PostgreSQL, Redis and Keycloak. Open `http://localhost:5236/scalar/v1`; the printed Aspire dashboard URL provides telemetry. Startup applies pending migrations.

Alternatively, use Compose:

```bash
docker compose up --build
```

Open `http://localhost:8080/scalar/v1`. Both options expose Keycloak at `http://localhost:8180`; do not run them simultaneously.

In Scalar, sign in as **alice/alice**, select write/fulfillment scopes, create a product with stock `10`, order `2`, then pay → ship → complete, or cancel before shipment. [Request bodies, routes and token setup](docs/api-guide.md).

> [!WARNING]
> Committed credentials and HTTP identity settings are **development-only**. Never reuse them in production. API-only startup needs a PostgreSQL connection and JWT configuration.

## 🧰 Technology and libraries

ASP.NET Core 10 Minimal APIs expose versioned contracts. PostgreSQL stores aggregates; Keycloak supplies local OIDC tokens. Aspire **13.5.4** or Compose connects services; Terraform defines optional Azure infrastructure.

| Library | Description | Purpose |
|---|---|---|
| <img src="docs/assets/logos/csharp-original.svg" width="28" height="28" align="absmiddle" alt="C# logo" /> [C#](https://learn.microsoft.com/dotnet/csharp/) | Language | Typed, nullable-aware domain and application code |
| <img src="docs/assets/logos/dotnet-original.svg" width="28" height="28" align="absmiddle" alt=".NET logo" /> [.NET 10 / ASP.NET Core](https://learn.microsoft.com/aspnet/core/) | Runtime and web framework | Minimal APIs with versioned contracts |
| <img src="docs/assets/logos/postgresql-original.svg" width="28" height="28" align="absmiddle" alt="PostgreSQL mark; EF Core and Npgsql share this database symbol" /> [EF Core / Npgsql](https://www.npgsql.org/efcore/) | Object-relational mapping | PostgreSQL persistence and migrations |
| <img src="docs/assets/logos/category-validation.svg" width="28" height="28" align="absmiddle" alt="Generic validation icon" /> [FluentValidation †](https://docs.fluentvalidation.net/) | Request validation | Reject invalid commands before handlers |
| <img src="docs/assets/logos/redis-original.svg" width="28" height="28" align="absmiddle" alt="Redis logo; not HybridCache branding" /> [HybridCache / Redis](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid) | Two-tier read cache | Local L1, optional Redis L2 |
| <img src="docs/assets/logos/category-versioning.svg" width="28" height="28" align="absmiddle" alt="Generic versioning icon" /> [Asp.Versioning †](https://github.com/dotnet/aspnet-api-versioning) | API version selection | Query/header version contracts |
| <picture><source media="(prefers-color-scheme: dark)" srcset="docs/assets/logos/scalar-dark.svg" /><img src="docs/assets/logos/scalar-light.svg" width="28" height="28" align="absmiddle" alt="Scalar logo" /></picture> [Scalar](https://scalar.com/) | Interactive API reference | Explore OpenAPI and authenticate |
| <img src="docs/assets/logos/opentelemetry-original.svg" width="28" height="28" align="absmiddle" alt="OpenTelemetry telescope mark" /> [OpenTelemetry](https://opentelemetry.io/) | Telemetry instrumentation | Correlate logs, traces and metrics |
| <img src="docs/assets/logos/dotnet-original.svg" width="28" height="28" align="absmiddle" alt=".NET shared platform mark; not Aspire branding" /> [Aspire *](https://aspire.dev/) | Local orchestration | Connect services and diagnostics |
| <img src="docs/assets/logos/xunit.svg" width="28" height="28" align="absmiddle" alt="Unmodified xUnit.net logo" /> [xUnit v3](https://xunit.net/) | Test runner | Execute unit and integration suites |
| <img src="docs/assets/logos/category-testing.svg" width="28" height="28" align="absmiddle" alt="Generic testing icon" /> [Shouldly / NSubstitute †](https://github.com/shouldly/shouldly) | Assertions and test doubles | Readable expectations; isolate collaborators |
| <picture><source media="(prefers-color-scheme: dark)" srcset="docs/assets/logos/testcontainers-icon-dark.svg" /><img src="docs/assets/logos/testcontainers-icon.svg" width="28" height="28" align="absmiddle" alt="Testcontainers mark" /></picture> [Testcontainers](https://dotnet.testcontainers.org/) | Container-based fixtures | Test against real PostgreSQL |
| <img src="docs/assets/logos/category-testing.svg" width="28" height="28" align="absmiddle" alt="Generic testing icon" /> [Respawn / NetArchTest †](https://github.com/jbogard/Respawn) | Database reset and architecture rules | Isolate tests; enforce layer boundaries |
| <img src="docs/assets/logos/docker-original.svg" width="28" height="28" align="absmiddle" alt="Docker logo" /> [Docker / Compose](https://docs.docker.com/compose/) | Containers | Run the API and its dependencies locally |
| <img src="docs/assets/logos/azure-original.svg" width="28" height="28" align="absmiddle" alt="Azure logo" /> [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/) | Cloud hosting | Optional production deployment target |
| <img src="docs/assets/logos/terraform-original.svg" width="28" height="28" align="absmiddle" alt="Terraform logo" /> [Terraform](https://developer.hashicorp.com/terraform) | Infrastructure as code | Define the optional Azure infrastructure |

† Neutral category icon, not dedicated library branding. * Shared .NET platform mark. Database/cache marks identify PostgreSQL/Redis, not every library in their row. [Asset sources and notices](docs/assets/logos/README.md).

Commands and queries use an **in-house mediator**, not MediatR: logging → validation → handler. Typed `Result<T>` failures become ProblemDetails responses. [Package pins](Directory.Packages.props) are authoritative; grouped test-library links: [NSubstitute](https://nsubstitute.github.io/), [NetArchTest](https://github.com/BenMorris/NetArchTest).

## 📐 Architecture

### 🧱 Code dependencies

The core references neither Infrastructure nor Presentation. Application consumes repository contracts; Infrastructure implements them; Presentation composes the API.

![Compile-time dependencies across Clean Architecture layers](docs/assets/architecture-layers.svg)

| Layer | Responsibility |
|---|---|
| Presentation | HTTP, authentication and composition |
| Application | Use cases, validation and event handlers |
| Domain | Aggregates, value objects and business rules |
| Infrastructure | Persistence, repositories, cache and current user |
| SharedKernel | Results, entities and messaging primitives |

Architecture tests enforce reference boundaries and conventions. Order lines store product IDs plus purchase-time snapshots, not mutable catalog navigation. [Full references and request sequence](docs/development-guide.md#project-dependencies).

### 🐳 Local runtime

![API runtime with identity, persistence, cache and telemetry](docs/assets/runtime-topology.svg)

Runtime arrows describe interactions, not project references. AppHost orchestrates services; ServiceDefaults configures telemetry and resilience. Redis is optional outside orchestrators; Compose has no Aspire dashboard.

**Palette:** indigo = delivery/client; blue = application/API; teal = domain/state; amber = infrastructure/external services; slate = shared/platform. Cancellation uses rose as a terminal exception. Figures adapt to light/dark themes.

## ✅ Quality and delivery

Run from the repository root; integration tests require Docker:

```bash
dotnet build CleanArchitecture.slnx
dotnet test CleanArchitecture.slnx
dotnet format CleanArchitecture.slnx --verify-no-changes
```

CI checks build, tests, formatting, API snapshots, architecture, vulnerabilities and infrastructure. Required coverage is **85% line / 80% branch**; mutation score **60%**. These are gates, not claimed measurements. [Actual CI results and artifacts](https://github.com/mbarretot/dotnet-clean-architecture/actions/workflows/ci.yml).

Successful `main` CI publishes signed GHCR images with SBOM/provenance. **Azure rollout is manual.** Terraform does not provision Keycloak or configure JWT; production requires identity, secrets, networking and a migration strategy.

## 📚 Go deeper

- [API guide](docs/api-guide.md): endpoints, example requests, local tokens and production identity.
- [Development guide](docs/development-guide.md): dependencies, test/coverage/load commands, signed images and Azure limitations.
- [Architecture decisions](docs/adr/README.md): rationale, alternatives and consequences.
- [HTTP collection](src/CleanArchitecture.Presentation/CleanArchitecture.Presentation.http): runnable API requests.
