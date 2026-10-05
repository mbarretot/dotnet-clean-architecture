using CleanArchitecture.Application.Products.GetProducts;
using CleanArchitecture.Application.UnitTests.TestDoubles;
using CleanArchitecture.Domain.Products;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.GetProducts;

public class GetProductsQueryHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();

    private GetProductsQueryHandler CreateSut() => new(_productRepository, TestHybridCache.Create());

    [Fact]
    public async Task Handle_ReturnsMappedProducts()
    {
        var first = Product.Create("First", "Description", Money.Create(10, "USD").Value, Sku.Create("SKU-1").Value).Value;
        var second = Product.Create("Second", "Description", Money.Create(20, "USD").Value, Sku.Create("SKU-2").Value).Value;
        _productRepository.SearchAsync(Arg.Any<ProductSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([first, second]);
        var sut = CreateSut();

        var result = await sut.Handle(new GetProductsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.Select(response => response.Name).ShouldBe(["First", "Second"]);
    }

    [Fact]
    public async Task Handle_WithNoProducts_ReturnsEmptyList()
    {
        _productRepository.SearchAsync(Arg.Any<ProductSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();

        var result = await sut.Handle(new GetProductsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null, ProductSortOrder.NameAscending)]
    [InlineData("name", ProductSortOrder.NameAscending)]
    [InlineData("-name", ProductSortOrder.NameDescending)]
    [InlineData("PRICE", ProductSortOrder.PriceAscending)]
    [InlineData(" -price ", ProductSortOrder.PriceDescending)]
    public async Task Handle_TranslatesTheQueryIntoSearchCriteria(string? sort, ProductSortOrder expectedSortOrder)
    {
        _productRepository.SearchAsync(Arg.Any<ProductSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();

        await sut.Handle(new GetProductsQuery(2, 5, "  key  ", 10m, 50m, sort), TestContext.Current.CancellationToken);

        await _productRepository.Received(1).SearchAsync(
            new ProductSearchCriteria("key", 10m, 50m, expectedSortOrder, 2, 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBlankSearch_SearchesWithoutATerm()
    {
        _productRepository.SearchAsync(Arg.Any<ProductSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();

        await sut.Handle(new GetProductsQuery(Search: "   "), TestContext.Current.CancellationToken);

        await _productRepository.Received(1).SearchAsync(
            Arg.Is<ProductSearchCriteria>(criteria => criteria.Search == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTheSameQueryTwice_ReadsTheRepositoryOnce()
    {
        _productRepository.SearchAsync(Arg.Any<ProductSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();

        await sut.Handle(new GetProductsQuery(Search: "mouse"), TestContext.Current.CancellationToken);
        await sut.Handle(new GetProductsQuery(Search: "MOUSE"), TestContext.Current.CancellationToken);
        await sut.Handle(new GetProductsQuery(Search: "keyboard"), TestContext.Current.CancellationToken);

        await _productRepository.Received(2).SearchAsync(Arg.Any<ProductSearchCriteria>(), Arg.Any<CancellationToken>());
    }
}
