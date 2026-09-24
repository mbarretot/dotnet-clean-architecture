# 7. Optimistic concurrency with PostgreSQL `xmin`

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Two requests can load the same product and both save, the second silently overwriting the first. Guarding against
lost updates usually means a version column that the application must maintain and the domain model must carry.
PostgreSQL already maintains a per-row version: the `xmin` system column changes on every update.

## Decision

Map `xmin` as an EF Core concurrency token on `Product`, as a shadow property so the domain model stays unaware of
it.

- In [`ApplicationDbContext.OnModelCreating`](../../src/CleanArchitecture.Infrastructure/Persistence/ApplicationDbContext.cs)
  a `uint` shadow property `Version` maps to column `xmin` (type `xid`), `ValueGeneratedOnAddOrUpdate`,
  `IsConcurrencyToken`.
- The mapping is applied only when the provider is Npgsql. SQLite, used by the interceptor unit tests
  ([`SqliteApplicationDbContextFixture`](../../tests/CleanArchitecture.Infrastructure.UnitTests/Persistence/Interceptors/SqliteApplicationDbContextFixture.cs)),
  has no equivalent. That is why the mapping lives in the context rather than in
  [`ProductConfiguration`](../../src/CleanArchitecture.Infrastructure/Persistence/Configurations/ProductConfiguration.cs).
- No migration adds a column: `xmin` exists on every PostgreSQL table.

## Consequences

**Positive**

- Lost updates between load and save are detected: EF Core adds `xmin` to the `UPDATE ... WHERE` clause and throws
  `DbUpdateConcurrencyException` when no row matches.
- Zero schema cost and nothing for the domain or application layers to maintain.

**Negative**

- Scope is limited to a single unit of work. The version is not exposed to clients (no `ETag` / `If-Match`), so two
  users editing the same product in separate requests still get last-write-wins.
- `DbUpdateConcurrencyException` is not translated to a `Result`; it reaches the global exception handler as a 500
  rather than a 409 (see [ADR-0004](0004-result-pattern.md)).
- Behaviour is provider-specific: tests on SQLite do not exercise it, and there is no dedicated concurrency test.
- `xmin` is a PostgreSQL implementation detail (it can also change on operations such as `VACUUM FULL`); moving to
  another database means a real version column.

## Alternatives considered

- **An explicit `Version`/`RowVersion` column maintained by the app.** Portable, but a schema column and
  application code for something PostgreSQL already provides.
- **Pessimistic locking (`SELECT ... FOR UPDATE`).** Strong guarantees, but holds locks and needs raw SQL or
  provider-specific APIs.
- **No concurrency control.** Simplest; accepts silent lost updates.
