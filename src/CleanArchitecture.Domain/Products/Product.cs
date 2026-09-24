using CleanArchitecture.Domain.Products.Events;
using CleanArchitecture.SharedKernel.Entities;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Domain.Products;

public sealed class Product : AggregateRoot, ISoftDeletable
{
    private Product(Guid id, string name, string description, Money price, Sku sku)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        Sku = sku;
    }

    /// <summary>For EF Core only: <see cref="Price"/> is an owned-type navigation, which constructor binding can't populate.</summary>
    private Product()
    {
        Name = string.Empty;
        Description = string.Empty;
        Price = null!;
        Sku = null!;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public Money Price { get; private set; }

    public Sku Sku { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    /// <summary>Raises <see cref="ProductCreatedDomainEvent"/>.</summary>
    public static Result<Product> Create(string name, string description, Money price, Sku sku)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Product>(ProductErrors.NameRequired);
        }

        var product = new Product(Guid.NewGuid(), name.Trim(), description.Trim(), price, sku);
        product.Raise(new ProductCreatedDomainEvent(product.Id));

        return product;
    }

    /// <summary>Raises <see cref="ProductUpdatedDomainEvent"/>.</summary>
    public Result Update(string name, string description, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(ProductErrors.NameRequired);
        }

        Name = name.Trim();
        Description = description.Trim();
        Price = price;

        Raise(new ProductUpdatedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the product and raises <see cref="ProductDeletedDomainEvent"/>; idempotent.
    /// <see cref="DeletedOnUtc"/> and <see cref="DeletedBy"/> are stamped by persistence, like the audit columns.
    /// </summary>
    public void Delete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;

        Raise(new ProductDeletedDomainEvent(Id));
    }
}
