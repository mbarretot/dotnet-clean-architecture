# 16. Order lifecycle as an explicit state machine

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

Placement/cancellation alone do not demonstrate a full aggregate lifecycle. Legal transitions, permissions and reserved-stock effects must be explicit.

## Decision

Centralize transitions in Order: `Placed → Paid → Shipped → Completed`; cancellation is allowed only from Placed/Paid. Emit a specific event per transition and store statuses as strings, requiring no new status migration.

## Outcome

Invalid transitions return 409; repeated cancellation retains `Order.AlreadyCancelled`. Owners pay/cancel with `orders:write` (other owners see 404); `orders:fulfill` permits shipping/completing any customer’s order.

## Consequences

### Benefits

- A table-driven test covers the legal/illegal state transitions.
- Separate customer/fulfillment permissions keep authorization visible in routes; cancellation releases stock only before shipment.

### Trade-offs

- Pay records state only: no provider, amount validation or idempotency key; shipping likewise has no carrier integration.
- Audit modification fields do not provide per-transition timestamps.
- Returns and refunds remain out of scope.

## Alternatives

- **State-machine library:** adds a dependency for a small fixed lifecycle.
- **Generic status PATCH:** fewer endpoints, but authorization must inspect the target status in the body.

## References

- [State machine](../../src/CleanArchitecture.Domain/Orders/Order.cs)
- [Transition tests](../../tests/CleanArchitecture.Application.UnitTests/Domain/Orders/OrderLifecycleTests.cs)
- [Fulfillment scopes](../../deploy/keycloak/clean-architecture-realm.json)
- [ADR-0009](0009-jwt-bearer-scopes-secure-by-default.md)
- [ADR-0015](0015-product-stock-reserved-with-the-order.md)
