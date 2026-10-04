# 17. HybridCache for product reads, invalidated by domain events

- **Status:** Accepted
- **Date:** 2026-10-04

## Context

Catalog reads (`GET /api/products`, `GET /api/products/{id}`) dominate traffic and change rarely, while every write
already raises a domain event after it commits ([ADR-0006](0006-domain-events-via-savechanges-interceptor.md)).

## Decision

Cache product query results with `Microsoft.Extensions.Caching.Hybrid` (in-process L1, optional Redis L2) and drop
them by tag whenever a product changes.

- [`GetProductByIdQueryHandler`](../../src/CleanArchitecture.Application/Products/GetProductById/GetProductByIdQueryHandler.cs)
  and [`GetProductsQueryHandler`](../../src/CleanArchitecture.Application/Products/GetProducts/GetProductsQueryHandler.cs)
  depend on the `HybridCache` abstraction (no Infrastructure reference) and cache `ProductResponse` DTOs, never
  entities. Keys come from [`ProductCacheKeys`](../../src/CleanArchitecture.Application/Products/ProductCacheKeys.cs);
  list keys include every filter, so each page/search/sort combination is its own entry. Every entry carries the
  `products` tag.
- [`ProductCacheInvalidationHandler`](../../src/CleanArchitecture.Application/Products/EventHandlers/ProductCacheInvalidationHandler.cs)
  handles the created, updated, deleted and stock-changed events with `RemoveByTagAsync("products")`. Because events
  are dispatched after `SaveChanges`, the cache never sees an uncommitted change.
- [`AddInfrastructure`](../../src/CleanArchitecture.Infrastructure/DependencyInjection.cs) registers HybridCache
  (5 min expiry, 1 min in-process) and, when `ConnectionStrings:Cache` is set, StackExchange.Redis as the
  distributed tier. The Aspire AppHost and Docker Compose provide Redis; Terraform takes an optional
  `cache_connection_string`. Without it the API caches in process only.
- The integration-test host clears the cache (`RemoveByTagAsync("*")`) whenever Respawn resets the database.

## Consequences

**Positive**

- Repeated reads skip the database; HybridCache also collapses concurrent misses for the same key into one query.
- Writes cannot leave stale entries behind on the instance that handled them, and the policy is a single class.

**Negative**

- Invalidation is coarse: any product change empties every product entry, including unrelated searches.
- With several replicas and no Redis, another replica can serve a stale entry for up to the 1 minute L1 lifetime.
  Tag invalidation does reach other replicas through Redis, but only after their L1 entries expire.
- Unknown ids are cached as "not found" until the next product change or expiry.

## Alternatives considered

- **Output caching at the HTTP layer.** Simpler, but keyed by URL and user, and blind to domain events.
- **`IMemoryCache` / `IDistributedCache` directly.** No tags and no stampede protection; HybridCache wraps both.
- **Per-product tags.** Finer invalidation, but every list entry would need the tags of all products it contains.
