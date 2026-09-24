using System.Linq.Expressions;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CleanArchitecture.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public const string SoftDeleteFilter = "SoftDelete";

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        if (!Database.IsNpgsql())
        {
            // SQLite cannot ORDER BY a text DateTimeOffset; the binary form sorts correctly.
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
