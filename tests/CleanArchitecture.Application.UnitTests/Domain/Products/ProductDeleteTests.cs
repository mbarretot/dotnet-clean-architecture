using CleanArchitecture.Domain.Products;
using CleanArchitecture.Domain.Products.Events;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Domain.Products;

public class ProductDeleteTests
{
    private static Product CreateProduct() =>
        Product.Create("Name", "Description", Money.Create(10, "USD").Value, Sku.Create("SKU-1").Value).Value;

    [Fact]
    public void Create_ReturnsProductThatIsNotDeleted()
    {
        var product = CreateProduct();

        product.IsDeleted.ShouldBeFalse();
        product.DeletedOnUtc.ShouldBeNull();
        product.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void Delete_MarksProductDeletedAndRaisesProductDeletedDomainEvent()
    {
        var product = CreateProduct();
        product.ClearDomainEvents();

        product.Delete();

        product.IsDeleted.ShouldBeTrue();
        product.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductDeletedDomainEvent>()
            .ProductId.ShouldBe(product.Id);
    }

    [Fact]
    public void Delete_LeavesDeletionTimestampAndActorForPersistenceToStamp()
    {
        var product = CreateProduct();

        product.Delete();

        product.DeletedOnUtc.ShouldBeNull();
        product.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_IsIdempotentAndDoesNotRaiseAnotherEvent()
    {
        var product = CreateProduct();
        product.Delete();
        product.ClearDomainEvents();

        product.Delete();

        product.IsDeleted.ShouldBeTrue();
        product.DomainEvents.ShouldBeEmpty();
    }
}
