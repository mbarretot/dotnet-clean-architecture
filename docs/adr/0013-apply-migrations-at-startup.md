# 13. Apply pending EF Core migrations at API startup

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

Every way of running the API (Aspire, Docker Compose, Azure Container Apps, `dotnet run`) starts against a database
that may be empty or behind the current model. Requiring a separate `dotnet ef database update` step before each
run is easy to forget and needs the EF tools wherever the API runs.

## Decision

The API applies pending migrations itself when it starts, before serving requests.

- [`Program.cs`](../../src/CleanArchitecture.Presentation/Program.cs) calls `ApplyPendingMigrationsAsync()` only when
  the Presentation assembly is the entry assembly, so `WebApplicationFactory` hosts in tests do not migrate
  implicitly ([`ApiFactory`](../../tests/CleanArchitecture.IntegrationTests/Infrastructure/ApiFactory.cs) migrates
  explicitly instead).
- [`MigrationExtensions`](../../src/CleanArchitecture.Infrastructure/Persistence/MigrationExtensions.cs) lists pending
  migrations, logs them, and calls `MigrateAsync`; with nothing pending it only logs. It works through
  [`IMigrationRunner`](../../src/CleanArchitecture.Infrastructure/Persistence/IMigrationRunner.cs) so the logic is
  unit-tested ([`MigrationExtensionsTests`](../../tests/CleanArchitecture.Infrastructure.UnitTests/Persistence/MigrationExtensionsTests.cs)).
- Applying migrations explicitly with `dotnet ef database update` remains possible, for example from CI.

## Consequences

**Positive**

- A fresh database is usable as soon as the API is reachable, in every environment, with no manual step.
- Schema and code are deployed together by the same image.

**Negative**

- The application's database login needs DDL permissions in every environment, including production.
- A failing migration stops the API from starting; startup time grows with migration work.
- With several replicas, each instance attempts to migrate on start; the design relies on EF Core's own handling
  of concurrent `Migrate` calls rather than a dedicated migration job.
- Destructive or long-running migrations run implicitly on deploy, without a separate approval step.

## Alternatives considered

- **Manual `dotnet ef database update`.** Explicit control; an extra step for every developer and deployment.
- **A dedicated migration job or init container** (or an EF migration bundle). Separates privileges and runs once
  per deployment; more deployment plumbing than this reference currently has.
- **Idempotent SQL scripts applied by the pipeline.** Reviewable SQL; needs a database-capable pipeline stage.
