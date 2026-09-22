using CleanArchitecture.Application.Products.GetProducts;
using CleanArchitecture.Domain.Products;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.GetProducts;

public class GetProductsQueryHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();

    private GetProductsQueryHandler CreateSut() => new(_productRepository);

    [Fact]
    public async Task Handle_ReturnsMappedProducts()
    {
        var first = Product.Create("First", "Description", Money.Create(10, "USD").Value, Sku.Create("SKU-1").Value).Value;
        var second = Product.Create("Second", "Description", Money.Create(20, "USD").Value, Sku.Create("SKU-2").Value).Value;
        _productRepository.GetAllAsync(1, 20, Arg.Any<CancellationToken>()).Returns([first, second]);
        var sut = CreateSut();

        var result = await sut.Handle(new GetProductsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.Select(response => response.Name).ShouldBe(["First", "Second"]);
    }

    [Fact]
    public async Task Handle_WithNoProducts_ReturnsEmptyList()
    {
        _productRepository.GetAllAsync(1, 20, Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();

        var result = await sut.Handle(new GetProductsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}
