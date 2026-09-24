# Architecture Decision Records

Significant architectural decisions for this repository, one per file. The format and rules are described in
[ADR-0001](0001-record-architecture-decisions.md): each record states its context, the decision, positive and
negative consequences, and the alternatives considered, with links to the code that implements it.

| # | Title | Status | Date |
|---|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted | 2026-09-24 |
| [0002](0002-clean-architecture-layering.md) | Clean Architecture layering with an executable dependency rule | Accepted | 2026-09-21 |
| [0003](0003-in-house-mediator.md) | In-house mediator instead of MediatR | Accepted | 2026-09-21 |
| [0004](0004-result-pattern.md) | Result pattern for expected failures, exceptions for the unexpected | Accepted | 2026-09-21 |
| [0005](0005-minimal-apis-with-endpoint-discovery.md) | Minimal APIs with `IEndpoint` discovery | Accepted | 2026-09-21 |
| [0006](0006-domain-events-via-savechanges-interceptor.md) | Domain events dispatched in-process by an EF Core `SaveChanges` interceptor | Accepted | 2026-09-21 |
| [0007](0007-optimistic-concurrency-xmin.md) | Optimistic concurrency with PostgreSQL `xmin` | Accepted | 2026-09-21 |
| [0008](0008-soft-delete-with-named-query-filter.md) | Soft delete with a named global query filter and a filtered unique SKU index | Accepted | 2026-09-24 |
| [0009](0009-jwt-bearer-scopes-secure-by-default.md) | Provider-agnostic JWT bearer, scope-based policies, secure by default | Accepted | 2026-09-24 |
| [0010](0010-keycloak-local-identity-provider.md) | Keycloak as the local identity provider | Accepted | 2026-09-24 |
| [0011](0011-integration-tests-testcontainers.md) | Integration tests with WebApplicationFactory, Testcontainers, Respawn, and self-minted JWTs | Accepted | 2026-09-24 |
| [0012](0012-aspire-compose-and-azure-container-apps.md) | Local orchestration with .NET Aspire and Docker Compose; deploy to Azure Container Apps with Terraform | Accepted | 2026-09-21 |
| [0013](0013-apply-migrations-at-startup.md) | Apply pending EF Core migrations at API startup | Accepted | 2026-09-24 |
| [0014](0014-order-aggregate-references-products-by-id.md) | Order aggregate references products by id and snapshots name and price | Accepted | 2026-09-24 |

## Adding a decision

1. Copy the structure of an existing ADR into `NNNN-short-title.md` with the next free number.
2. Link the code that implements the decision.
3. Add a row to the table above.
4. To change an accepted decision, write a new ADR and mark the old one *Superseded by ADR-NNNN*.
