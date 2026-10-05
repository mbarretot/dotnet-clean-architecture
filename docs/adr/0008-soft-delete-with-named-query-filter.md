# 8. Soft delete with a named global query filter and a filtered unique SKU index

- **Status:** Accepted
- **Date:** 2026-09-24

## Context

Deactivation left products visible and SKUs unavailable; hard deletion loses history. Deleted products should read as absent while retained records preserve the past.

## Decision

Soft-delete through `ISoftDeletable`, apply a named global filter, and enforce SKU uniqueness only where `is_deleted = FALSE`. Audit deletion once; migrate old flags using `is_deleted = NOT is_active`, not a rename.

## Outcome

Deleted products return 404 and their SKUs become reusable. A query can bypass `ApplicationDbContext.SoftDeleteFilter` independently of future filters.

## Consequences

### Benefits

- History remains available without per-query exclusion logic.
- New soft-deletable roots inherit filtering automatically.

### Trade-offs

- Rows accumulate and need a retention policy; reporting must account for hidden records.
- No restore endpoint exists.
- Migration rollback can fail after a deleted SKU is reused, because the old unfiltered index cannot accept duplicates.

## Alternatives

- **Hard delete:** smaller tables, but loses history/reference continuity.
- **Manual active-flag checks:** easy to omit and leak hidden rows.
- **Archive table:** separates retained data, but adds copy/migration complexity.

## References

- [Soft-delete contract](../../src/CleanArchitecture.SharedKernel/Entities/ISoftDeletable.cs)
- [Query filter](../../src/CleanArchitecture.Infrastructure/Persistence/ApplicationDbContext.cs)
- [SKU index](../../src/CleanArchitecture.Infrastructure/Persistence/Configurations/ProductConfiguration.cs)
- [Migration](../../src/CleanArchitecture.Infrastructure/Persistence/Migrations/20260924154223_SoftDeleteProducts.cs)
- [Deletion auditing](../../src/CleanArchitecture.Infrastructure/Persistence/Interceptors/AuditableEntitySaveChangesInterceptor.cs)
