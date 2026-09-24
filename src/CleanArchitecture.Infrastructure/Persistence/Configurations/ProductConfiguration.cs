using CleanArchitecture.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

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

        builder.Property(product => product.IsDeleted)
            .HasColumnName(IsDeletedColumn)
            .IsRequired();
        builder.Property(product => product.DeletedBy).HasMaxLength(256);

        builder.Property(product => product.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(product => product.ModifiedBy).HasMaxLength(256);

        builder.Ignore(product => product.DomainEvents);
    }
}
