using System.Linq.Expressions;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        if (!Database.IsNpgsql())
        {
            // SQLite (used by repository and interceptor tests) cannot ORDER BY a DateTimeOffset stored as text; the
            // binary form sorts correctly and round-trips the offset. PostgreSQL keeps its native timestamptz.
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        }

        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ApplySoftDeleteFilter(modelBuilder);

        if (Database.IsNpgsql())
        {
            ApplyConcurrencyToken(modelBuilder);
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Adds an xmin shadow property for optimistic concurrency to every <see cref="AggregateRoot"/>: the aggregate is the
    /// consistency boundary, so that is where concurrent writes are detected. Npgsql-only since SQLite (used by
    /// interceptor tests) has no equivalent.
    /// </summary>
    private static void ApplyConcurrencyToken(ModelBuilder modelBuilder)
    {
        var aggregateRootTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => entityType.BaseType is null
                && !entityType.IsOwned()
                && typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
            .Select(entityType => entityType.ClrType)
            .ToList();

        foreach (var clrType in aggregateRootTypes)
        {
            modelBuilder.Entity(clrType)
                .Property<uint>("Version")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        }
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
