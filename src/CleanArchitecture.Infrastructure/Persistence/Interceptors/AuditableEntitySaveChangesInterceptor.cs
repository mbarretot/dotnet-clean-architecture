using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CleanArchitecture.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps who/when before the write (<see cref="SavingChanges"/>), so stamped values are persisted: creation and
/// modification for <see cref="IAuditable"/>, deletion for <see cref="ISoftDeletable"/> entities.
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor(
    IDateTimeProvider dateTimeProvider,
    ICurrentUser currentUser) : SaveChangesInterceptor
{
    /// <summary>Actor recorded for changes made outside an authenticated request.</summary>
    public const string SystemUser = "system";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampAuditableEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAuditableEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampAuditableEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = dateTimeProvider.UtcNow;
        var userId = currentUser.UserId ?? SystemUser;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditable.CreatedBy)).CurrentValue = userId;
                    entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(IAuditable.ModifiedBy)).CurrentValue = userId;
                    entry.Property(nameof(IAuditable.ModifiedAt)).CurrentValue = now;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }
        }

        // Stamped only once, when the flag first flips: a later save of an already deleted entity keeps the original stamp.
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified
                && entry.Entity is { IsDeleted: true, DeletedOnUtc: null })
            {
                entry.Property(nameof(ISoftDeletable.DeletedOnUtc)).CurrentValue = now;
                entry.Property(nameof(ISoftDeletable.DeletedBy)).CurrentValue = userId;
            }
        }
    }
}
