using CleanArchitecture.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="OrderLine"/>s live in their own table, owned by the order through a required, cascading foreign key.
/// A line stores the product id as a plain column with no foreign key to <c>products</c>: the aggregates stay
/// independent, and a later product change or soft delete never touches an existing order.
/// </summary>
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

        // xmin concurrency token is configured in ApplicationDbContext instead (provider-conditional).
        builder.Ignore(order => order.Total);
        builder.Ignore(order => order.DomainEvents);
    }
}
