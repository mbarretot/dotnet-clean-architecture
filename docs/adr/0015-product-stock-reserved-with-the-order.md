# 15. Product stock is reserved in the same transaction as the order

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

Orders could be placed for any quantity: the catalog had no notion of units on hand. Adding stock means an order and
the products it reserves must change together, or a crash between the two leaves either an order without a
reservation or a reservation without an order. [ADR-0014](0014-order-aggregate-references-products-by-id.md) chose
one aggregate per transaction; that rule now has a deliberate exception.

## Decision

`Product` owns its stock; placing an order reserves it and cancelling releases it, inside the same unit of work as the
order change.

- [`Product`](../../src/CleanArchitecture.Domain/Products/Product.cs) has `StockQuantity` (zero by default, never
  negative) and three operations: `SetStock` (replace, for catalog managers), `ReserveStock` (fails with the conflict
  `Product.InsufficientStock`) and `ReleaseStock`. Each raises `ProductStockChangedDomainEvent`.
- [`PlaceOrderCommandHandler`](../../src/CleanArchitecture.Application/Orders/PlaceOrder/PlaceOrderCommandHandler.cs)
  places the order first (so lines for the same product are already merged), then reserves each line's quantity and
  saves once. Any failure returns before `SaveChanges`, so nothing is persisted.
- [`CancelOrderCommandHandler`](../../src/CleanArchitecture.Application/Orders/CancelOrder/CancelOrderCommandHandler.cs)
  releases each line's quantity in the same save. A product soft-deleted since the order was placed is skipped.
- `PUT /api/products/{id}/stock` sets the stock and requires `products:write`; `ProductResponse` exposes
  `stockQuantity`, and `POST /api/products` accepts an optional initial `stockQuantity`.
- Migration [`AddProductStock`](../../src/CleanArchitecture.Infrastructure/Persistence/Migrations/20261004183546_AddProductStock.cs)
  adds `products.stock_quantity` (existing rows start at 0) and the check constraint
  `ck_products_stock_quantity_non_negative` as a database backstop.
- Concurrent reservations of the same product are serialized by the `xmin` token
  ([ADR-0007](0007-optimistic-concurrency-xmin.md)). The loser's `DbUpdateConcurrencyException` is now translated by
  [`GlobalExceptionHandler`](../../src/CleanArchitecture.Presentation/Middleware/GlobalExceptionHandler.cs) into a
  409 `Concurrency.Conflict` problem instead of a 500, so clients can retry.

## Consequences

**Positive**

- Overselling is impossible: the domain rejects it, the check constraint backs it, and optimistic concurrency stops
  two requests from both taking the last unit.
- Order and stock changes are atomic without a saga or an outbox.

**Negative**

- One transaction now writes several aggregates. Contention on a popular product becomes 409s under load, and the
  aggregates cannot move to separate databases without redesign (an outbox plus compensation).
- Existing products start with zero stock after the migration and cannot be ordered until restocked.
- The 409 for concurrency is generic: the client cannot tell which product conflicted.

## Alternatives considered

- **Eventual consistency via `OrderPlacedDomainEvent`.** Keeps one aggregate per transaction, but events are
  dispatched after the commit ([ADR-0006](0006-domain-events-via-savechanges-interceptor.md)), so a failed
  reservation would leave an accepted order behind.
- **A separate `Inventory` aggregate.** Cleaner if stock grows warehouses or batches; premature for a single number.
- **Pessimistic locking (`SELECT ... FOR UPDATE`).** No 409s, but holds row locks and needs provider-specific SQL.
