using CleanArchitecture.Domain.Products;
using CleanArchitecture.Domain.Products.Events;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Domain.Products;

public class ProductTests
{
    private static readonly Money Price = Money.Create(10m, "USD").Value;

    private static readonly Sku Sku = Sku.Create("SKU-1").Value;

    [Fact]
    public void Create_TrimsAndRaisesProductCreated()
    {
        var product = Product.Create("  Keyboard ", " Mechanical ", Price, Sku).Value;

        product.Name.ShouldBe("Keyboard");
        product.Description.ShouldBe("Mechanical");
        product.DomainEvents.ShouldHaveSingleItem().ShouldBe(new ProductCreatedDomainEvent(product.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutName_Fails(string name) =>
        Product.Create(name, "d", Price, Sku).Error.ShouldBe(ProductErrors.NameRequired);

    [Fact]
    public void Update_ChangesFieldsAndRaisesProductUpdated()
    {
        var product = Product.Create("Keyboard", "d", Price, Sku).Value;
        product.ClearDomainEvents();
        var newPrice = Money.Create(12m, "EUR").Value;

        product.Update(" Mouse ", " Wireless ", newPrice).IsSuccess.ShouldBeTrue();

        product.Name.ShouldBe("Mouse");
        product.Description.ShouldBe("Wireless");
        product.Price.ShouldBe(newPrice);
        product.DomainEvents.ShouldHaveSingleItem().ShouldBe(new ProductUpdatedDomainEvent(product.Id));
    }

    [Fact]
    public void Update_WithoutName_FailsAndChangesNothing()
    {
        var product = Product.Create("Keyboard", "d", Price, Sku).Value;
        product.ClearDomainEvents();

        product.Update(" ", "other", Price).Error.ShouldBe(ProductErrors.NameRequired);

        product.Name.ShouldBe("Keyboard");
        product.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SetStock_ToZero_IsAllowed()
    {
        var product = Product.Create("Keyboard", "d", Price, Sku, stockQuantity: 3).Value;

        product.SetStock(0).IsSuccess.ShouldBeTrue();

        product.StockQuantity.ShouldBe(0);
    }
}
