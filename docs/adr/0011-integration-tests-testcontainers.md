# 11. Integration tests with WebApplicationFactory, Testcontainers, Respawn, and self-minted JWTs

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

HTTP middleware, migrations, indexes and interceptors need end-to-end checks. SQLite/in-memory providers miss PostgreSQL behavior; an external issuer introduces an unrelated dependency.

## Decision

Use WebApplicationFactory with disposable PostgreSQL 17, Testcontainers and self-signed test JWTs. Share one sequential factory, apply migrations explicitly and reset rows with Respawn before each test.

## Outcome

Actual routing, JwtBearer validation, policies and SQL run together. The IntegrationTests environment excludes development secrets; resets retain schema/migration history and clear HybridCache.

## Consequences

### Benefits

- No shared database or identity-provider state leaks between runs.
- One container plus row resets avoids creating a database for every test.

### Trade-offs

- Docker must be available locally and in CI.
- Sequential collection execution limits parallelism.
- HS256 test tokens exercise validation, not real issuers’ asymmetric JWKS discovery.

## Alternatives

- **SQLite/in-memory:** quicker, but omits PostgreSQL-specific behavior; SQLite remains useful for unit fixtures.
- **Fake authentication:** bypasses the validator under test.
- **Database per test:** stronger isolation, but slower setup.
- **Long-lived shared database:** no startup cost, but leaked state between developers/runs.

## References

- [API factory](../../tests/CleanArchitecture.IntegrationTests/Infrastructure/ApiFactory.cs)
- [Sequential collection](../../tests/CleanArchitecture.IntegrationTests/Infrastructure/IntegrationTestCollection.cs)
- [Database-reset base](../../tests/CleanArchitecture.IntegrationTests/Infrastructure/IntegrationTest.cs)
- [Test tokens](../../tests/CleanArchitecture.IntegrationTests/Infrastructure/TestJwtTokens.cs)
- [ADR-0007](0007-optimistic-concurrency-xmin.md)
