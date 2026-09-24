using CleanArchitecture.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary><see cref="Sku"/> is a converted column, <see cref="Money"/> an owned type; both have private ctors whose parameter names match their properties, so EF Core's constructor binding materializes them with no reflection helpers.</summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private const string IsDeletedColumn = "is_deleted";

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(product => product.Sku)
            .HasConversion(sku => sku.Value, value => Sku.Create(value).Value)
            .HasColumnName("sku")
            .HasMaxLength(64)
            .IsRequired();

        // Unique among non-deleted products only, so a soft-deleted product's SKU can be reused. The filter is
        // portable SQL (PostgreSQL and SQLite) and matches the soft-delete query filter the SKU existence check runs under.
        builder.HasIndex(product => product.Sku)
            .IsUnique()
            .HasFilter($"{IsDeletedColumn} = FALSE");

        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            priceBuilder.Property(money => money.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(product => product.Price).IsRequired();

        // Explicit column name: the SKU index filter references it, and must not depend on the naming convention in use.
        builder.Property(product => product.IsDeleted)
            .HasColumnName(IsDeletedColumn)
            .IsRequired();
        builder.Property(product => product.DeletedBy).HasMaxLength(256);

        builder.Property(product => product.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(product => product.ModifiedBy).HasMaxLength(256);

        // xmin concurrency token is configured in ApplicationDbContext instead (provider-conditional).
        builder.Ignore(product => product.DomainEvents);
    }
}
