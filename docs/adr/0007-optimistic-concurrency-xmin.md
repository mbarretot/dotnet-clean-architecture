# 7. Optimistic concurrency with PostgreSQL `xmin`

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Overlapping load/save operations can overwrite an aggregate without detecting another writer. PostgreSQL already exposes a row version through `xmin`.

## Decision

Map `xmin` as a generated `uint` shadow concurrency token for every aggregate root, only under Npgsql. This detects conflicts without adding a version field to the domain or schema.

## Outcome

EF includes the token in update predicates and throws `DbUpdateConcurrencyException` when the loaded version no longer matches; Product and Order share the convention.

## Consequences

### Benefits

- Concurrent updates are detected without application-maintained version columns.
- New aggregate roots inherit the mapping.

### Trade-offs

- No ETag/If-Match contract: stale client edits across separate requests are not detected by a prior client version.
- Exceptions originally surfaced as 500; [ADR-0015](0015-product-stock-reserved-with-the-order.md) later maps them to 409. Clients must reload/retry.
- PostgreSQL-specific behavior is not exercised by SQLite unit fixtures; the original decision had no dedicated concurrency test.

## Alternatives

- **Explicit version column:** portable, but needs schema and maintenance.
- **Pessimistic locking:** holds locks and needs provider-specific access.
- **No token:** simpler, but permits silent lost updates.

## References

- [Shadow-token mapping](../../src/CleanArchitecture.Infrastructure/Persistence/ApplicationDbContext.cs)
- [SQLite fixture](../../tests/CleanArchitecture.Infrastructure.UnitTests/Persistence/Interceptors/SqliteApplicationDbContextFixture.cs)
- [Current error mapping](../../src/CleanArchitecture.Presentation/Middleware/GlobalExceptionHandler.cs)
- [ADR-0004](0004-result-pattern.md)
