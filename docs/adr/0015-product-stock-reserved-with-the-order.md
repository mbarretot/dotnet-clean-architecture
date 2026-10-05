# 15. Product stock is reserved in the same transaction as the order

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

Orders previously accepted quantities without inventory checks. Reservation and order persistence must succeed together; this deliberately excepts [ADR-0014](0014-order-aggregate-references-products-by-id.md)’s original one-aggregate transaction rule.

## Decision

Product owns nonnegative stock, initially zero. Reserve merged order quantities and save Product/Order changes once; cancellation releases stock in the same unit of work, skipping products deleted since placement.

## Outcome

Insufficient stock returns `Product.InsufficientStock` (409) before any save. Domain operations raise stock events, a check constraint rejects negatives, and xmin conflicts become generic 409 `Concurrency.Conflict` responses requiring reload/retry.

## Consequences

### Benefits

- Stock and order writes are atomic without a saga or outbox.
- Domain validation plus optimistic concurrency guard concurrent reservations against overselling.

### Trade-offs

- Popular products create write contention; 409 does not identify the conflicting product.
- Existing migrated products start at zero and need restocking; catalog writes can replace stock.
- Multiple aggregates share a database transaction; separating their databases requires redesign/compensation.

## Alternatives

- **Post-commit event reservation:** may leave an accepted order without stock because [ADR-0006](0006-domain-events-via-savechanges-interceptor.md) is not durable.
- **Inventory aggregate:** useful for warehouses/batches, premature for one quantity.
- **Pessimistic locks:** reduce optimistic conflicts, but hold locks and need provider-specific SQL.

## References

- [Stock rules](../../src/CleanArchitecture.Domain/Products/Product.cs)
- [Placement](../../src/CleanArchitecture.Application/Orders/PlaceOrder/PlaceOrderCommandHandler.cs)
- [Cancellation](../../src/CleanArchitecture.Application/Orders/CancelOrder/CancelOrderCommandHandler.cs)
- [Stock migration](../../src/CleanArchitecture.Infrastructure/Persistence/Migrations/20261004183546_AddProductStock.cs)
- [Conflict mapping](../../src/CleanArchitecture.Presentation/Middleware/GlobalExceptionHandler.cs)
- [ADR-0007](0007-optimistic-concurrency-xmin.md)
