# 6. Domain events dispatched in-process by an EF Core `SaveChanges` interceptor

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Aggregates raise events, but consumers must not react to unsaved changes. Publishing explicitly in every use case is easy to forget.

## Decision

Publish buffered domain events through a scoped EF Core `SavedChangesAsync` interceptor. After a successful save, it clears aggregate buffers and dispatches each event through the in-house publisher.

## Outcome

Async unit-of-work saves trigger in-process notification handlers automatically; business handlers do not orchestrate publication.

## Consequences

### Benefits

- Publication follows successful persistence.
- One interceptor applies the rule consistently.

### Trade-offs

- No durable outbox or retry: crashes after save and handler failures can lose events; an outbox remains a reliability follow-up.
- A throwing handler can produce HTTP 500 after data has already committed.
- Handlers add request latency; synchronous `SaveChanges()` is not intercepted.

## Alternatives

- **Before-save dispatch:** handlers may react to changes later rolled back.
- **Explicit handler publication:** visible, but duplicated and forgettable.
- **Transactional outbox now:** durable/at-least-once, but more machinery than this single-service reference currently uses.

## References

- [Event buffer](../../src/CleanArchitecture.SharedKernel/Entities/AggregateRoot.cs)
- [Interceptor](../../src/CleanArchitecture.Infrastructure/Persistence/Interceptors/DispatchDomainEventsInterceptor.cs)
- [Interceptor tests](../../tests/CleanArchitecture.Infrastructure.UnitTests/Persistence/Interceptors/DispatchDomainEventsInterceptorTests.cs)
- [ADR-0003](0003-in-house-mediator.md)
- [ADR-0017](0017-hybridcache-for-product-reads.md)
