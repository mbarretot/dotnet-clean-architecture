# 11. Integration tests with WebApplicationFactory, Testcontainers, Respawn, and self-minted JWTs

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

Unit tests cover each layer, but several behaviours only exist when everything runs together: routing,
authentication and the fallback policy, ProblemDetails mapping, EF Core migrations, the `xmin` token, the filtered
SKU index, and the interceptors. SQLite or in-memory providers cannot reproduce PostgreSQL-specific behaviour, and
an external identity provider would make tests slow and flaky.

## Decision

Test the real API over HTTP, in memory, against a throwaway PostgreSQL container, authenticating with JWTs the
tests sign themselves.

- [`ApiFactory`](../../tests/CleanArchitecture.IntegrationTests/Infrastructure/ApiFactory.cs) extends
  `WebApplicationFactory<Program>`, starts `postgres:17-alpine` via Testcontainers, applies migrations once, and
  runs under a dedicated `IntegrationTests` environment so developer user-secrets are not loaded.
- [`IntegrationTestCollection`](../../tests/CleanArchitecture.IntegrationTests/Infrastructure/IntegrationTestCollection.cs)
  shares one factory across all tests and runs them sequentially;
  [`IntegrationTest`](../../tests/CleanArchitecture.IntegrationTests/Infrastructure/IntegrationTest.cs) resets data
  with Respawn before each test, keeping the schema and `__EFMigrationsHistory`.
- [`TestJwtTokens`](../../tests/CleanArchitecture.IntegrationTests/Infrastructure/TestJwtTokens.cs) mints HS256
  tokens with a test-only key; the factory configures the matching issuer, audience and signing key in the
  `Authentication:Schemes:Bearer` shape. The API's JwtBearer pipeline and scope policy run unmodified; no test
  authentication handler replaces them.
- Tests are grouped by concern under
  [`tests/CleanArchitecture.IntegrationTests`](../../tests/CleanArchitecture.IntegrationTests): products lifecycle,
  errors, auditing, authorization, health, and OpenAPI.

## Consequences

**Positive**

- High confidence: the same middleware, policies, SQL, and migrations as production are exercised.
- Tests are hermetic: no shared database, no identity provider, no network beyond Docker.
- One container per run plus Respawn keeps the suite reasonably fast.

**Negative**

- Docker must be running for `dotnet test`, locally and in CI.
- Sequential execution inside the collection limits parallelism as the suite grows.
- Symmetric test signing differs from the asymmetric keys (JWKS discovery) real issuers use, so key discovery is not
  covered here.

## Alternatives considered

- **EF Core in-memory or SQLite.** Fast, no Docker; misses PostgreSQL behaviour (`xmin`, filtered indexes, types).
  SQLite is still used for interceptor unit tests.
- **A fake authentication handler.** Simpler tokens; bypasses the real JwtBearer validation being tested.
- **A new database per test.** Maximum isolation; much slower than resetting rows with Respawn.
- **A shared long-lived test database.** No container start-up; state leaks between runs and developers.
