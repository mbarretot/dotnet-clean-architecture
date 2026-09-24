using CleanArchitecture.Domain.Orders;
using CleanArchitecture.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines");

        builder.HasKey(line => line.Id);

        builder.Property(line => line.Id).ValueGeneratedNever();

        builder.Property(line => line.ProductId).IsRequired();

        builder.Property(line => line.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(line => line.Quantity).IsRequired();

        builder.OwnsOne(line => line.UnitPrice, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                .HasColumnName("unit_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            priceBuilder.Property(money => money.Currency)
                .HasColumnName("unit_price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(line => line.UnitPrice).IsRequired();

        builder.Property(line => line.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(line => line.ModifiedBy).HasMaxLength(256);

        builder.Ignore(line => line.LineTotal);
    }
}
