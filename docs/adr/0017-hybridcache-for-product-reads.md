# 17. HybridCache for product reads, invalidated by domain events

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

Product reads can repeat unchanged data while product writes already raise post-save events. Caching should remain independent of Infrastructure references in Application.

## Decision

Cache ProductResponse DTOs with HybridCache: in-process L1 and optional Redis L2. Key every page/filter/sort combination separately and invalidate the products tag on created/updated/deleted/stock-changed events.

## Outcome

Repeated reads and concurrent same-key misses can reuse cached results. Infrastructure sets five-minute expiry and one-minute L1; orchestration supplies Redis, Terraform accepts a connection string, and test resets clear cache.

## Consequences

### Benefits

- Queries avoid caching tracked entities.
- One event-handler class centralizes invalidation after persistence.

### Trade-offs

- Any product write invalidates all product entries, including unrelated searches.
- Other replicas can serve stale L1 entries until expiry, including when Redis is present; invalidation is not instantaneous across replicas.
- Unknown IDs cache as not-found until invalidation/expiry; delivery inherits [ADR-0006](0006-domain-events-via-savechanges-interceptor.md)’s in-process failure limitations.

## Alternatives

- **HTTP output caching:** URL/user-driven and disconnected from domain events.
- **Direct memory/distributed caches:** require implementing tags/stampede handling.
- **Per-product tags:** finer, but lists need tags for every included product.

## References

- [Query caching](../../src/CleanArchitecture.Application/Products/GetProducts/GetProductsQueryHandler.cs)
- [Keys](../../src/CleanArchitecture.Application/Products/ProductCacheKeys.cs)
- [Invalidation](../../src/CleanArchitecture.Application/Products/EventHandlers/ProductCacheInvalidationHandler.cs)
- [Registration](../../src/CleanArchitecture.Infrastructure/DependencyInjection.cs)
- [Invalidation tests](../../tests/CleanArchitecture.Application.UnitTests/Products/EventHandlers/ProductCacheInvalidationHandlerTests.cs)
