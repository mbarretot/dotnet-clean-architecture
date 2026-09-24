# 8. Soft delete with a named global query filter and a filtered unique SKU index

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

`DELETE` used to "deactivate" a product through an `is_active` flag: the product stayed visible in every read (with
`isActive: false`) and its SKU stayed taken by the unique index. Deleting rows outright would lose history and break
references from past orders. A deleted product must behave as gone (404, SKU reusable) while its record is kept.

## Decision

Soft-delete products, hide deleted rows with a named EF Core global query filter, and scope SKU uniqueness to
non-deleted rows.

- [`ISoftDeletable`](../../src/CleanArchitecture.SharedKernel/Entities/ISoftDeletable.cs) exposes `IsDeleted`,
  `DeletedOnUtc`, `DeletedBy`. [`Product.Delete()`](../../src/CleanArchitecture.Domain/Products/Product.cs) sets the
  flag idempotently and raises `ProductDeletedDomainEvent`; `DELETE /api/products/{id}` goes through
  [`DeleteProductCommandHandler`](../../src/CleanArchitecture.Application/Products/DeleteProduct/DeleteProductCommandHandler.cs).
- [`AuditableEntitySaveChangesInterceptor`](../../src/CleanArchitecture.Infrastructure/Persistence/Interceptors/AuditableEntitySaveChangesInterceptor.cs)
  stamps `DeletedOnUtc` / `DeletedBy` once, when the flag first flips.
- [`ApplicationDbContext`](../../src/CleanArchitecture.Infrastructure/Persistence/ApplicationDbContext.cs) adds the
  filter `!IsDeleted` to every root `ISoftDeletable` entity under the name `SoftDeleteFilter`, so a single query can
  bypass just that filter with `IgnoreQueryFilters([ApplicationDbContext.SoftDeleteFilter])`.
- [`ProductConfiguration`](../../src/CleanArchitecture.Infrastructure/Persistence/Configurations/ProductConfiguration.cs)
  makes the SKU index unique with the filter `is_deleted = FALSE`, so a deleted product's SKU can be reused.
- Migration [`SoftDeleteProducts`](../../src/CleanArchitecture.Infrastructure/Persistence/Migrations/20260924154223_SoftDeleteProducts.cs)
  is hand-edited: it adds `is_deleted`, back-fills it as `NOT is_active`, then drops `is_active`, instead of the
  scaffolded rename that would have inverted every row.

## Consequences

**Positive**

- History and references are preserved; reads, updates and deletes of a deleted product return 404 with no extra
  code in handlers or repositories.
- New soft-deletable entities are filtered automatically by implementing the interface.
- Naming the filter lets other filters (e.g. multi-tenancy) be added and bypassed independently.

**Negative**

- Deleted rows stay in the table; data grows and eventually needs a retention policy.
- Every query pays the filter; forgetting that it exists can surprise someone writing reporting queries.
- The migration's `Down` fails if a deleted product's SKU was reused, because the unfiltered index cannot hold both.
- There is no restore endpoint yet.

## Alternatives considered

- **Hard delete.** Simplest and keeps tables small; loses history and breaks references.
- **Keep the `is_active` flag checked by hand.** Every query must remember it; easy to leak inactive products.
- **Archive table.** Moves deleted rows elsewhere; more migrations and copy logic for little gain here.
