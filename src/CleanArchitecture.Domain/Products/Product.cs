using CleanArchitecture.Domain.Products.Events;
using CleanArchitecture.SharedKernel.Entities;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Domain.Products;

public sealed class Product : AggregateRoot, ISoftDeletable
{
    private Product(Guid id, string name, string description, Money price, Sku sku, int stockQuantity)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        Sku = sku;
        StockQuantity = stockQuantity;
    }

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

    public int StockQuantity { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static Result<Product> Create(string name, string description, Money price, Sku sku, int stockQuantity = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Product>(ProductErrors.NameRequired);
        }

        if (stockQuantity < 0)
        {
            return Result.Failure<Product>(ProductErrors.StockNegative);
        }

        var product = new Product(Guid.NewGuid(), name.Trim(), description.Trim(), price, sku, stockQuantity);
        product.Raise(new ProductCreatedDomainEvent(product.Id));

        return product;
    }

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

    public Result SetStock(int quantity)
    {
        if (quantity < 0)
        {
            return Result.Failure(ProductErrors.StockNegative);
        }

        if (quantity == StockQuantity)
        {
            return Result.Success();
        }

        StockQuantity = quantity;

        Raise(new ProductStockChangedDomainEvent(Id, StockQuantity));

        return Result.Success();
    }

    public Result ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(ProductErrors.StockQuantityNotPositive);
        }

        if (quantity > StockQuantity)
        {
            return Result.Failure(ProductErrors.InsufficientStock(Id, quantity, StockQuantity));
        }

        StockQuantity -= quantity;

        Raise(new ProductStockChangedDomainEvent(Id, StockQuantity));

        return Result.Success();
    }

    public Result ReleaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(ProductErrors.StockQuantityNotPositive);
        }

        StockQuantity += quantity;

        Raise(new ProductStockChangedDomainEvent(Id, StockQuantity));

        return Result.Success();
    }

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
