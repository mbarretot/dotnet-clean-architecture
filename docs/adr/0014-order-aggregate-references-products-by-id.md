# 14. Order aggregate references products by id and snapshots name and price

- **Status:** Accepted; amended by [ADR-0015](0015-product-stock-reserved-with-the-order.md) (stock reservation
  writes products in the order's transaction)
- **Date:** 2026-09-24

## Context

Orders must preserve purchased names/prices after catalog changes or deletion. Live Product navigation would couple aggregate lifecycles and mutable purchase history.

## Decision

Let Order own its lines and retain only ProductId plus name/price snapshots. Initially, placement saved only Order; [ADR-0015](0015-product-stock-reserved-with-the-order.md) deliberately amends the one-aggregate transaction rule for atomic stock reservation.

## Outcome

Placement requires a customer, positive lines and one currency, merges duplicates, and rejects missing/deleted products. Orders load with their lines; owner-only reads/cancellation hide other customers’ orders as 404.

## Consequences

### Benefits

- Catalog changes cannot rewrite purchase history.
- Architecture tests prohibit cross-aggregate entity navigation; lines cascade from orders and own their Money values.

### Trade-offs

- No product foreign key: database integrity does not guarantee ProductId existence; snapshots intentionally ignore later corrections.
- Placement loads products individually; batch loading remains a follow-up.
- Non-Npgsql DateTimeOffset conversion exists for SQLite ordering tests; xmin protects roots under PostgreSQL.

## Alternatives

- **Product navigation:** convenient joins, but couples aggregates/history.
- **Foreign key only:** preserves integrity, but ties table lifecycles and restricts deletion.
- **Live prices:** avoids duplication, but changes historical totals.

## References

- [Order](../../src/CleanArchitecture.Domain/Orders/Order.cs)
- [Placement](../../src/CleanArchitecture.Application/Orders/PlaceOrder/PlaceOrderCommandHandler.cs)
- [Order-line mapping](../../src/CleanArchitecture.Infrastructure/Persistence/Configurations/OrderLineConfiguration.cs)
- [No-product-FK migration](../../src/CleanArchitecture.Infrastructure/Persistence/Migrations/20260924161833_AddOrders.cs)
- [Boundary test](../../tests/CleanArchitecture.ArchitectureTests/AggregateBoundaryTests.cs)
- [ADR-0008](0008-soft-delete-with-named-query-filter.md)
- [ADR-0009](0009-jwt-bearer-scopes-secure-by-default.md)
