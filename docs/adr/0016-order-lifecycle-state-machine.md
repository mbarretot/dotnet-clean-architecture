# 16. Order lifecycle as an explicit state machine

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

An order could only be placed or cancelled. A reference domain should show how an aggregate guards a lifecycle:
which transitions are legal, who may trigger them, and what happens to side effects such as reserved stock.

## Decision

[`Order`](../../src/CleanArchitecture.Domain/Orders/Order.cs) moves through
`Placed → Paid → Shipped → Completed`, and can be `Cancelled` from `Placed` or `Paid`.

- `Pay()`, `Ship()`, `Complete()` and `Cancel()` all go through one private `TransitionTo(target, allowedFrom, event)`.
  An illegal move returns the conflict `Order.InvalidStatusTransition` and changes nothing; cancelling twice keeps
  the more specific `Order.AlreadyCancelled`. Each transition raises its own event (`OrderPaidDomainEvent`,
  `OrderShippedDomainEvent`, `OrderCompletedDomainEvent`, `OrderCancelledDomainEvent`).
- Endpoints: `POST /api/orders/{id}/pay` belongs to the customer (`orders:write`, other customers' orders are 404).
  `POST /api/orders/{id}/ship` and `/complete` are fulfilment actions on any order and require the new
  `orders:fulfill` scope, declared in the [Keycloak realm](../../deploy/keycloak/clean-architecture-realm.json) and
  granted to the DEV ONLY service client ([ADR-0009](0009-jwt-bearer-scopes-secure-by-default.md)).
- Cancelling releases the order's reserved stock ([ADR-0015](0015-product-stock-reserved-with-the-order.md)); a shipped
  order can no longer be cancelled, so shipped stock is never returned.
- `OrderStatus` is stored as a string, so the new values need no migration.

## Consequences

**Positive**

- Every legal transition is visible in one place and covered by a table-driven test
  ([`OrderLifecycleTests`](../../tests/CleanArchitecture.Application.UnitTests/Domain/Orders/OrderLifecycleTests.cs)).
- Customer and fulfilment permissions are separate scopes, not a role check inside handlers.

**Negative**

- `Pay()` is a status change only: there is no payment provider, amount check or idempotency key.
- No timestamps per transition (`PaidAt`, `ShippedAt`); the audit columns record only the latest modification.
- Returns and refunds (`Completed → Returned`) are out of scope.

## Alternatives considered

- **A state-machine library (e.g. Stateless).** Adds a dependency for five states and four transitions.
- **One generic `PATCH /api/orders/{id}` with a target status.** Fewer endpoints, but authorization would depend on
  the request body instead of the route.
