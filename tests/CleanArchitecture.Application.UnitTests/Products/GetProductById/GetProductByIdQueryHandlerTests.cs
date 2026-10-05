using CleanArchitecture.Application.Products.GetProductById;
using CleanArchitecture.Application.UnitTests.TestDoubles;
using CleanArchitecture.Domain.Products;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.GetProductById;

public class GetProductByIdQueryHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly HybridCache _cache = TestHybridCache.Create();

    private GetProductByIdQueryHandler CreateSut() => new(_productRepository, _cache);

    private static Product NewProduct() =>
        Product.Create("Name", "Description", Money.Create(10, "USD").Value, Sku.Create("SKU-1").Value, 7).Value;

    [Fact]
    public async Task Handle_WithExistingProduct_ReturnsMappedResponse()
    {
        var product = NewProduct();
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var sut = CreateSut();

        var result = await sut.Handle(new GetProductByIdQuery(product.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(product.Id);
        result.Value.Name.ShouldBe("Name");
        result.Value.Sku.ShouldBe("SKU-1");
        result.Value.StockQuantity.ShouldBe(7);
    }

    [Fact]
    public async Task Handle_WithUnknownProduct_ReturnsNotFound()
    {
        _productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        var sut = CreateSut();
        var id = Guid.NewGuid();

        var result = await sut.Handle(new GetProductByIdQuery(id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProductErrors.NotFound(id));
    }

    [Fact]
    public async Task Handle_CalledTwice_ReadsTheRepositoryOnce()
    {
        var product = NewProduct();
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var sut = CreateSut();

        await sut.Handle(new GetProductByIdQuery(product.Id), TestContext.Current.CancellationToken);
        var second = await sut.Handle(new GetProductByIdQuery(product.Id), TestContext.Current.CancellationToken);

        second.Value.Id.ShouldBe(product.Id);
        await _productRepository.Received(1).GetByIdAsync(product.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AfterTheProductsTagIsInvalidated_ReadsTheRepositoryAgain()
    {
        var product = NewProduct();
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var sut = CreateSut();

        await sut.Handle(new GetProductByIdQuery(product.Id), TestContext.Current.CancellationToken);
        await _cache.RemoveByTagAsync("products", TestContext.Current.CancellationToken);
        await sut.Handle(new GetProductByIdQuery(product.Id), TestContext.Current.CancellationToken);

        await _productRepository.Received(2).GetByIdAsync(product.Id, Arg.Any<CancellationToken>());
    }
}
