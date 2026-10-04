using CleanArchitecture.Domain.Products;
using CleanArchitecture.Domain.Products.Events;
using CleanArchitecture.SharedKernel.Results;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Domain.Products;

public class ProductStockTests
{
    private static Product NewProduct(int stock)
    {
        var product = Product.Create(
            "Keyboard", "d", Money.Create(10m, "USD").Value, Sku.Create("SKU-1").Value, stock).Value;
        product.ClearDomainEvents();

        return product;
    }

    [Fact]
    public void Create_WithoutStock_StartsAtZero() =>
        Product.Create("Keyboard", "d", Money.Create(10m, "USD").Value, Sku.Create("SKU-1").Value)
            .Value.StockQuantity.ShouldBe(0);

    [Fact]
    public void Create_WithNegativeStock_Fails() =>
        Product.Create("Keyboard", "d", Money.Create(10m, "USD").Value, Sku.Create("SKU-1").Value, -1)
            .Error.ShouldBe(ProductErrors.StockNegative);

    [Fact]
    public void ReserveStock_WithEnoughUnits_DecrementsAndRaisesStockChanged()
    {
        var product = NewProduct(5);

        product.ReserveStock(5).IsSuccess.ShouldBeTrue();

        product.StockQuantity.ShouldBe(0);
        product.DomainEvents.ShouldHaveSingleItem().ShouldBe(new ProductStockChangedDomainEvent(product.Id, 0));
    }

    [Fact]
    public void ReserveStock_WithTooFewUnits_FailsWithConflictAndKeepsTheStock()
    {
        var product = NewProduct(2);

        var result = product.ReserveStock(3);

        result.Error.ShouldBe(ProductErrors.InsufficientStock(product.Id, 3, 2));
        result.Error.ErrorType.ShouldBe(ErrorType.Conflict);
        product.StockQuantity.ShouldBe(2);
        product.DomainEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReserveAndRelease_WithNonPositiveQuantity_Fail(int quantity)
    {
        var product = NewProduct(5);

        product.ReserveStock(quantity).Error.ShouldBe(ProductErrors.StockQuantityNotPositive);
        product.ReleaseStock(quantity).Error.ShouldBe(ProductErrors.StockQuantityNotPositive);
        product.StockQuantity.ShouldBe(5);
    }

    [Fact]
    public void ReleaseStock_IncrementsAndRaisesStockChanged()
    {
        var product = NewProduct(1);

        product.ReleaseStock(4).IsSuccess.ShouldBeTrue();

        product.StockQuantity.ShouldBe(5);
        product.DomainEvents.ShouldHaveSingleItem().ShouldBe(new ProductStockChangedDomainEvent(product.Id, 5));
    }

    [Fact]
    public void SetStock_ReplacesTheQuantity()
    {
        var product = NewProduct(1);

        product.SetStock(9).IsSuccess.ShouldBeTrue();

        product.StockQuantity.ShouldBe(9);
        product.DomainEvents.ShouldHaveSingleItem().ShouldBe(new ProductStockChangedDomainEvent(product.Id, 9));
    }

    [Fact]
    public void SetStock_ToTheSameQuantity_RaisesNothing()
    {
        var product = NewProduct(3);

        product.SetStock(3).IsSuccess.ShouldBeTrue();

        product.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SetStock_Negative_Fails()
    {
        var product = NewProduct(3);

        product.SetStock(-1).Error.ShouldBe(ProductErrors.StockNegative);
        product.StockQuantity.ShouldBe(3);
    }
}
