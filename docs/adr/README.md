# Architecture Decision Records

Read the purpose, choice and trade-offs behind the implementation. Each record follows [ADR-0001](0001-record-architecture-decisions.md): **Context → Decision → Outcome → Consequences (Benefits / Trade-offs) → Alternatives → References**.

Status and date remain historical metadata; Outcome describes enabled behavior, not a measured result.

All 19 records are **Accepted**; dates preserve their original decision history.

| # | Decision | Date |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record decisions | 2026-09-24 |
| [0002](0002-clean-architecture-layering.md) | Executable layer boundaries | 2026-09-21 |
| [0003](0003-in-house-mediator.md) | In-house mediator | 2026-09-21 |
| [0004](0004-result-pattern.md) | Expected failures as results | 2026-09-21 |
| [0005](0005-minimal-apis-with-endpoint-discovery.md) | Endpoint discovery | 2026-09-21 |
| [0006](0006-domain-events-via-savechanges-interceptor.md) | In-process domain events | 2026-09-21 |
| [0007](0007-optimistic-concurrency-xmin.md) | xmin concurrency | 2026-09-21 |
| [0008](0008-soft-delete-with-named-query-filter.md) | Soft deletion and SKU reuse | 2026-09-24 |
| [0009](0009-jwt-bearer-scopes-secure-by-default.md) | JWT and scope authorization | 2026-09-24 |
| [0010](0010-keycloak-local-identity-provider.md) | Local Keycloak | 2026-09-24 |
| [0011](0011-integration-tests-testcontainers.md) | PostgreSQL integration tests | 2026-09-24 |
| [0012](0012-aspire-compose-and-azure-container-apps.md) | Local/cloud orchestration | 2026-09-21 |
| [0013](0013-apply-migrations-at-startup.md) | Startup migrations | 2026-09-24 |
| [0014](0014-order-aggregate-references-products-by-id.md) | Order snapshots and boundaries | 2026-09-24 |
| [0015](0015-product-stock-reserved-with-the-order.md) | Atomic stock reservation | 2026-10-04 |
| [0016](0016-order-lifecycle-state-machine.md) | Order state machine | 2026-10-04 |
| [0017](0017-hybridcache-for-product-reads.md) | Product read caching | 2026-10-04 |
| [0018](0018-api-versioning.md) | Default API versioning | 2026-10-04 |
| [0019](0019-quality-gates-in-ci.md) | CI and supply-chain gates | 2026-10-04 |

## Maintaining the log

- Add one `NNNN-short-title.md` record per significant decision, with the next unused ID and concrete evidence links; update this index.
- Shorten or clarify wording without changing an accepted decision’s meaning or historical metadata.
- For a substantive change, write a new ADR and explicitly amend or supersede the earlier record.
