using CleanArchitecture.Application.Products.DeleteProduct;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Abstractions;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.DeleteProduct;

public class DeleteProductCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteProductCommandHandler CreateSut() => new(_productRepository, _unitOfWork);

    private static Product CreateExistingProduct() =>
        Product.Create("Name", "Description", Money.Create(10, "USD").Value, Sku.Create("SKU-1").Value).Value;

    [Fact]
    public async Task Handle_WithExistingProduct_SoftDeletesAndSaves()
    {
        var product = CreateExistingProduct();
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var sut = CreateSut();

        var result = await sut.Handle(new DeleteProductCommand(product.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        product.IsDeleted.ShouldBeTrue();
        _productRepository.DidNotReceive().Remove(Arg.Any<Product>());
        _productRepository.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownProduct_ReturnsNotFoundWithoutSaving()
    {
        _productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new DeleteProductCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
