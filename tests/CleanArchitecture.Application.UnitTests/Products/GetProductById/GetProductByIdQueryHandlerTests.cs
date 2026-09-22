using CleanArchitecture.Application.Products.GetProductById;
using CleanArchitecture.Domain.Products;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.GetProductById;

public class GetProductByIdQueryHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();

    private GetProductByIdQueryHandler CreateSut() => new(_productRepository);

    [Fact]
    public async Task Handle_WithExistingProduct_ReturnsMappedResponse()
    {
        var product = Product.Create("Name", "Description", Money.Create(10, "USD").Value, Sku.Create("SKU-1").Value).Value;
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var sut = CreateSut();

        var result = await sut.Handle(new GetProductByIdQuery(product.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(product.Id);
        result.Value.Name.ShouldBe("Name");
        result.Value.Sku.ShouldBe("SKU-1");
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
}
