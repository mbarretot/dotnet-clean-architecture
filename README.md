<div align="center">

<img src="docs/assets/clean-architecture-logo.png" alt="Clean Architecture logo" width="180" />

# .NET Clean Architecture

**A production-shaped reference for building maintainable .NET APIs with enforced boundaries.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/architecture-clean-0B7285?style=flat-square)](#architecture)
[![Aspire](https://img.shields.io/badge/.NET_Aspire-13.4-7B2CBF?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/aspire/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%20%7C%2017-4169E1?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-enabled-F5A800?style=flat-square&logo=opentelemetry&logoColor=black)](https://opentelemetry.io/)
[![Tests](https://img.shields.io/badge/tests-121_passing-2EA44F?style=flat-square)](#quality-gates)
[![License: MIT](https://img.shields.io/badge/license-MIT-22C55E?style=flat-square)](LICENSE)

[Architecture](#architecture) · [Request flow](#request-flow) · [Run](#run-it) · [Project map](#project-map) · [Delivery](#delivery)

</div>

> [!IMPORTANT]
> This is a complete reference implementation to study and adapt—not a `dotnet new` template.

## ✨ At a glance

| Area | What is implemented |
|---|---|
| **Architecture** | Clean Architecture, CQRS-style commands/queries, inward-only dependencies |
| **Domain** | Aggregates, value objects, domain events, `Result` / `Result<T>` |
| **API** | ASP.NET Core Minimal APIs, endpoint discovery, RFC 7807, OpenAPI + Scalar |
| **Data** | EF Core 10, PostgreSQL, migrations, auditing, optimistic concurrency |
| **Platform** | .NET Aspire, OpenTelemetry, Docker, Azure Container Apps, Terraform |
| **Quality** | xUnit v3, Shouldly, NSubstitute, NetArchTest, warnings as errors |

## 🏗️ Architecture

```mermaid
flowchart TB
    Client([Client]) --> Presentation[Presentation<br/>Minimal API · endpoints · ProblemDetails]

    Presentation --> Application[Application<br/>use cases · validation · pipeline behaviors]
    Presentation --> Infrastructure[Infrastructure<br/>EF Core · repositories · identity · time]
    Application --> Domain[Domain<br/>aggregates · value objects · events]
    Application --> SharedKernel[SharedKernel<br/>results · entities · in-house mediator]
    Infrastructure --> Application
    Infrastructure --> Domain
    Infrastructure --> SharedKernel
    Domain --> SharedKernel

    Infrastructure --> PostgreSQL[(PostgreSQL)]
    AppHost[.NET Aspire AppHost] -. orchestrates .-> Presentation
    AppHost -. provisions locally .-> PostgreSQL
    ServiceDefaults[ServiceDefaults<br/>OTel · discovery · resilience] -. configures .-> Presentation

    classDef core fill:#102a43,color:#fff,stroke:#38bdf8,stroke-width:2px;
    classDef outer fill:#f8fafc,color:#0f172a,stroke:#64748b;
    class Domain,Application,SharedKernel core;
    class Presentation,Infrastructure,AppHost,ServiceDefaults,PostgreSQL,Client outer;
```

| Project | Owns | May depend on |
|---|---|---|
| `SharedKernel` | Results, base entities, mediator primitives | — |
| `Domain` | Business rules and product aggregate | `SharedKernel` |
| `Application` | Commands, queries, handlers, validation | `Domain`, `SharedKernel` |
| `Infrastructure` | Persistence and external concerns | `Application`, `Domain`, `SharedKernel` |
| `Presentation` | HTTP composition root and endpoints | Application layers + `ServiceDefaults` |

The dependency rules are executable: `CleanArchitecture.ArchitectureTests` rejects invalid layer references and convention drift.

## 🔄 Request flow

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant API as Minimal API endpoint
    participant Sender as In-house ISender
    participant Pipeline as Logging → Validation
    participant Handler as Command / Query handler
    participant Domain as Aggregate
    participant Data as Repository + Unit of Work
    participant DB as PostgreSQL
    participant Events as Domain event publisher

    Client->>API: HTTP request
    API->>Sender: ICommand or IQuery
    Sender->>Pipeline: Dispatch request
    Pipeline->>Handler: Valid request
    Handler->>Domain: Execute business rule
    Handler->>Data: Persist changes
    Data->>DB: SaveChangesAsync
    DB-->>Data: Commit
    Data->>Events: Publish events after commit
    Handler-->>API: Result or Result<T>
    API-->>Client: HTTP response or ProblemDetails
```

- No MediatR dependency: `SharedKernel.Messaging` provides the mediator and pipeline.
- Expected failures travel as typed results; exceptions are handled globally.
- Domain events are dispatched by an EF Core interceptor **after** a successful commit.

## 🚀 Run it

**Prerequisites:** [.NET SDK 10.0.302](https://dotnet.microsoft.com/download/dotnet/10.0), Docker, and the [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/cli/overview) for the recommended path.

> [!NOTE]
> EF Core migrations are not applied automatically. Initialize a fresh database before using the product endpoints; the standalone flow below includes that step.

### 🟣 Aspire — recommended

```bash
aspire run
```

Starts the API, PostgreSQL, and the Aspire dashboard with logs, traces, and metrics.

### 🐳 Docker Compose

```bash
docker compose up --build
```

| Resource | URL |
|---|---|
| API | `http://localhost:8080` |
| Scalar API reference | `http://localhost:8080/scalar/v1` |
| OpenAPI document | `http://localhost:8080/openapi/v1.json` |

<details>
<summary><strong>Run with a separate PostgreSQL instance</strong></summary>

```bash
dotnet tool restore
dotnet ef database update \
  --project src/CleanArchitecture.Infrastructure \
  --startup-project src/CleanArchitecture.Infrastructure
dotnet run --project src/CleanArchitecture.Presentation
```

Set `ConnectionStrings:Database` through configuration or user secrets before starting the API.

</details>

## 🔌 API surface

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/products` | List products with pagination |
| `GET` | `/api/products/{id}` | Get one product |
| `POST` | `/api/products` | Create a product |
| `PUT` | `/api/products/{id}` | Update a product |
| `DELETE` | `/api/products/{id}` | Deactivate a product |

## 🗂️ Project map

```text
src/
├── CleanArchitecture.SharedKernel      # Results, entities, mediator
├── CleanArchitecture.Domain            # Product aggregate and domain rules
├── CleanArchitecture.Application       # Use cases and pipeline behaviors
├── CleanArchitecture.Infrastructure    # EF Core, PostgreSQL, repositories
├── CleanArchitecture.Presentation      # Minimal API, OpenAPI, Scalar
├── CleanArchitecture.ServiceDefaults   # OTel, discovery, resilience
└── CleanArchitecture.AppHost           # Aspire orchestration
tests/
├── *.UnitTests                         # Layer-focused tests
└── CleanArchitecture.ArchitectureTests # Dependency and convention rules
terraform/                              # Azure Container Apps + PostgreSQL
```

## ✅ Quality gates

```bash
dotnet build CleanArchitecture.slnx
dotnet test CleanArchitecture.slnx
dotnet format CleanArchitecture.slnx --verify-no-changes
```

| Gate | Coverage |
|---|---|
| Build | Nullable enabled, analyzers, deterministic output, warnings as errors |
| Tests | **121 tests** across five projects |
| Architecture | Layer, handler, repository, command/query, and mediator conventions |
| CI | Build, test, formatting, Docker build, Terraform format + validation |

## 📦 Delivery

```mermaid
flowchart LR
    PR[Pull request] --> CI[GitHub Actions CI]
    CI --> Tests[Build · tests · format]
    CI --> ImageCheck[Docker build check]
    CI --> IaCCheck[Terraform validation]
    Main[main] --> Gate[CI gate]
    Gate --> GHCR[(GHCR image)]
    Operator[Terraform apply] --> ACA[Azure Container Apps]
    GHCR --> ACA
    ACA --> PG[(PostgreSQL Flexible Server)]
    ACA --> Logs[Log Analytics]
```

- Pushes to `main` publish SHA and `latest` images to GHCR after CI succeeds.
- `terraform/` provisions Azure Container Apps, PostgreSQL Flexible Server, and Log Analytics.
- OTLP export is enabled whenever `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.
