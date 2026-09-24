using System.Linq.Expressions;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence;

/// <summary>Auditing and domain-event dispatch live in interceptors, not here.</summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Name of the global query filter that hides soft-deleted rows. Bypass it for a single query with
    /// <c>IgnoreQueryFilters([ApplicationDbContext.SoftDeleteFilter])</c>, leaving any other named filter active.
    /// </summary>
    public const string SoftDeleteFilter = "SoftDelete";

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ApplySoftDeleteFilter(modelBuilder);

        if (Database.IsNpgsql())
        {
            // xmin shadow property for optimistic concurrency; Npgsql-only since SQLite (used by interceptor tests) has no equivalent.
            modelBuilder.Entity<Product>()
                .Property<uint>("Version")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Adds <c>entity => !entity.IsDeleted</c> to every <see cref="ISoftDeletable"/> root entity type, so new ones need no extra wiring.</summary>
    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        var softDeletableTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => entityType.BaseType is null
                && !entityType.IsOwned()
                && typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            .Select(entityType => entityType.ClrType)
            .ToList();

        foreach (var clrType in softDeletableTypes)
        {
            var entity = Expression.Parameter(clrType, "entity");
            var isDeleted = Expression.Property(entity, nameof(ISoftDeletable.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(isDeleted), entity);

            modelBuilder.Entity(clrType).HasQueryFilter(SoftDeleteFilter, filter);
        }
    }
}
