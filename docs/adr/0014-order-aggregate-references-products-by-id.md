# 14. Order aggregate references products by id and snapshots name and price

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

Customers place orders for catalog products. An order line must show what was bought and at what price, even after
the product is renamed, repriced or soft-deleted ([ADR-0008](0008-soft-delete-with-named-query-filter.md)). A
navigation from `OrderLine` to `Product` would let one transaction load and modify two aggregates, and would make an
order's history depend on the product's current state.

## Decision

Model `Order` as its own aggregate root that owns its lines and refers to products by id only, copying the product's
name and price into each line when the order is placed.

- [`Order`](../../src/CleanArchitecture.Domain/Orders/Order.cs) is created only through `Order.Place`, which
  enforces a customer, at least one line, positive quantities and a single currency, merges lines for the same
  product and raises `OrderPlacedDomainEvent`. `Cancel()` raises `OrderCancelledDomainEvent` and fails with a
  conflict if the order is already cancelled.
- [`OrderLine`](../../src/CleanArchitecture.Domain/Orders/OrderLine.cs) holds `ProductId` plus the `ProductName`
  and `UnitPrice` snapshots; it is changed only through the order.
- [`PlaceOrderCommandHandler`](../../src/CleanArchitecture.Application/Orders/PlaceOrder/PlaceOrderCommandHandler.cs)
  coordinates the two aggregates: it loads each distinct product through `IProductRepository` (soft-deleted ones are
  not found, so they return 404), builds the snapshots, and saves only the new order.
- [`OrderConfiguration`](../../src/CleanArchitecture.Infrastructure/Persistence/Configurations/OrderConfiguration.cs)
  maps lines to `order_lines` with a cascading foreign key to `orders`;
  [`OrderLineConfiguration`](../../src/CleanArchitecture.Infrastructure/Persistence/Configurations/OrderLineConfiguration.cs)
  maps the price as an owned `Money` (`unit_price_amount`, `unit_price_currency`). Migration
  [`AddOrders`](../../src/CleanArchitecture.Infrastructure/Persistence/Migrations/20260924161833_AddOrders.cs)
  deliberately creates no foreign key from `order_lines.product_id` to `products`.
- [`OrderRepository`](../../src/CleanArchitecture.Infrastructure/Persistence/Repositories/OrderRepository.cs) always
  loads the whole aggregate, lines included.
- Orders belong to the caller's `sub`. Reading or cancelling another customer's order returns 404, so its existence
  is never revealed. Placing and cancelling require the `orders:write` scope
  ([ADR-0009](0009-jwt-bearer-scopes-secure-by-default.md)); reading requires an authenticated user.
- The `xmin` concurrency token now applies to every aggregate root, `Order` included
  ([ADR-0007](0007-optimistic-concurrency-xmin.md)).
- [`AggregateBoundaryTests`](../../tests/CleanArchitecture.ArchitectureTests/AggregateBoundaryTests.cs) fails
  if any Domain entity holds another aggregate root, or a collection of them, as a property.
- [`ApplicationDbContext.ConfigureConventions`](../../src/CleanArchitecture.Infrastructure/Persistence/ApplicationDbContext.cs)
  stores `DateTimeOffset` as binary when the provider is not Npgsql, so the SQLite repository tests can order
  orders by `CreatedAt`; PostgreSQL keeps `timestamptz`.

## Consequences

**Positive**

- An order is a stable historical record: catalog changes and soft deletes never rewrite it.
- Each transaction modifies one aggregate, and the boundary is enforced by a test rather than by convention.
- Ownership is checked in the handlers and hides other customers' orders behind 404.

**Negative**

- No referential integrity at the database level: nothing stops a `product_id` that never existed, and a hard delete
  of a product would leave lines pointing nowhere.
- Snapshots go stale by design: an order does not reflect a later price or name correction.
- Products are loaded one query at a time while placing an order; a batch load is a follow-up.
- The SQLite-only `DateTimeOffset` conversion lives in production code to serve tests.

## Alternatives considered

- **Navigation from `OrderLine` to `Product`.** Convenient joins and a real foreign key; couples the aggregates and
  shows the current product instead of what was bought.
- **Foreign key without a navigation.** Keeps integrity, but blocks hard-deleting products that have been ordered and
  ties the two tables' lifecycles together.
- **Read prices from the product at query time.** No duplicated data; order totals change whenever the catalog does.
