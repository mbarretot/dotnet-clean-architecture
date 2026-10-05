# 13. Apply pending EF Core migrations at API startup

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

An empty or outdated database makes local/deployed startup fail without a separate schema step. Requiring EF tooling everywhere adds setup work.

## Decision

Apply pending migrations before serving requests when Presentation is the entry assembly. Skip implicit migration under WebApplicationFactory; integration tests migrate explicitly, and manual EF updates remain supported.

## Outcome

MigrationExtensions logs pending migrations and invokes MigrateAsync; no pending work means logging only. IMigrationRunner makes this orchestration unit-testable.

## Consequences

### Benefits

- Fresh databases become usable with API startup.
- The deployed image carries both code and schema migrations.

### Trade-offs

- The API database login needs DDL privileges, including in production.
- Failed/long migrations stop or delay startup; destructive changes have no separate approval gate.
- Each replica attempts migration, relying on EF’s concurrency handling rather than a dedicated migration job.

## Alternatives

- **Manual EF update:** explicit, but an extra developer/deployment step.
- **Dedicated job/init container/bundle:** separates privileges and execution, but needs deployment plumbing.
- **Pipeline SQL:** reviewable, but requires database connectivity and another stage.

## References

- [Startup guard](../../src/CleanArchitecture.Presentation/Program.cs)
- [Migration runner](../../src/CleanArchitecture.Infrastructure/Persistence/MigrationExtensions.cs)
- [Runner contract](../../src/CleanArchitecture.Infrastructure/Persistence/IMigrationRunner.cs)
- [Migration tests](../../tests/CleanArchitecture.Infrastructure.UnitTests/Persistence/MigrationExtensionsTests.cs)
- [ADR-0011](0011-integration-tests-testcontainers.md)
