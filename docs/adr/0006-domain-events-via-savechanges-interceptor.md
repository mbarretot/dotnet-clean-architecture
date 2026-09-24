# 6. Domain events dispatched in-process by an EF Core `SaveChanges` interceptor

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Aggregates raise domain events (`ProductCreatedDomainEvent`, `ProductUpdatedDomainEvent`,
`ProductDeletedDomainEvent` in [`Domain/Products/Events`](../../src/CleanArchitecture.Domain/Products/Events)) that
other parts of the application react to. Handlers must not see an event for a change that was never saved, and
use-case handlers should not have to remember to publish events themselves.

## Decision

Collect and publish domain events from an EF Core interceptor after `SaveChangesAsync` succeeds.

- [`AggregateRoot`](../../src/CleanArchitecture.SharedKernel/Entities/AggregateRoot.cs) buffers events raised by the
  aggregate.
- [`DispatchDomainEventsInterceptor`](../../src/CleanArchitecture.Infrastructure/Persistence/Interceptors/DispatchDomainEventsInterceptor.cs)
  overrides `SavedChangesAsync` (not `SavingChangesAsync`), gathers events from tracked aggregates, clears them, and
  publishes each through `IPublisher` ([ADR-0003](0003-in-house-mediator.md)).
- It is registered as scoped in
  [`Infrastructure/DependencyInjection.cs`](../../src/CleanArchitecture.Infrastructure/DependencyInjection.cs), so
  `UnitOfWork.SaveChangesAsync` triggers dispatch without handlers knowing about it.
- Handlers derive from
  [`DomainEventHandler<T>`](../../src/CleanArchitecture.SharedKernel/Messaging/DomainEventHandler.cs), e.g.
  [`ProductCreatedDomainEventHandler`](../../src/CleanArchitecture.Application/Products/EventHandlers/ProductCreatedDomainEventHandler.cs).

## Consequences

**Positive**

- Events are published only for changes the database accepted.
- Use cases stay focused on the domain; dispatch is a persistence concern, applied uniformly.
- Covered by [`DispatchDomainEventsInterceptorTests`](../../tests/CleanArchitecture.Infrastructure.UnitTests/Persistence/Interceptors/DispatchDomainEventsInterceptorTests.cs).

**Negative**

- **Not durable.** Events live only in memory. If the process stops between commit and dispatch, or a handler
  fails, the event is lost; there is no retry.
- A handler that throws surfaces as an exception from `SaveChangesAsync` *after* the data was committed, so the
  client can receive a 500 for a change that succeeded.
- Handlers run synchronously inside the request, adding their latency to it.
- Only the async path is intercepted; a synchronous `SaveChanges()` would commit without dispatching.

**Follow-up:** a transactional outbox (persist events in the same transaction, dispatch from a background worker)
is the known next step when events must reach other services reliably.

## Alternatives considered

- **Dispatch in `SavingChangesAsync` (before commit).** Handlers can join the transaction, but they may react to a
  change that later rolls back.
- **Publish explicitly from each command handler.** Visible, but easy to forget and duplicated.
- **Transactional outbox now.** Durable and at-least-once; more moving parts than a single-service reference needs
  today.
