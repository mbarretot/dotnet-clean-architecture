using CleanArchitecture.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.CustomerId)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(order => order.CustomerId);

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.Property(order => order.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(order => order.ModifiedBy).HasMaxLength(256);

        builder.Ignore(order => order.Total);
        builder.Ignore(order => order.DomainEvents);
    }
}
