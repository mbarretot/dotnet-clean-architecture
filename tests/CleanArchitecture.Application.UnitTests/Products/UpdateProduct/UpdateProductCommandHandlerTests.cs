using CleanArchitecture.Application.Products.UpdateProduct;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Abstractions;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.UpdateProduct;

public class UpdateProductCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private UpdateProductCommandHandler CreateSut() => new(_productRepository, _unitOfWork, _dateTimeProvider);

    private static Product CreateExistingProduct() =>
        Product.Create("Old name", "Old description", Money.Create(10, "USD").Value, Sku.Create("SKU-1").Value).Value;

    [Fact]
    public async Task Handle_WithExistingProduct_UpdatesAndSaves()
    {
        var product = CreateExistingProduct();
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var now = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(now);
        var sut = CreateSut();
        var command = new UpdateProductCommand(product.Id, "New name", "New description", 20, "EUR");

        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        product.Name.ShouldBe("New name");
        product.ModifiedAt.ShouldBe(now);
        _productRepository.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownProduct_ReturnsNotFoundWithoutSaving()
    {
        _productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        var sut = CreateSut();
        var command = new UpdateProductCommand(Guid.NewGuid(), "New name", "New description", 20, "EUR");

        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ErrorType.ShouldBe(SharedKernel.Results.ErrorType.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPrice_ReturnsFailureWithoutSaving()
    {
        var product = CreateExistingProduct();
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var sut = CreateSut();
        var command = new UpdateProductCommand(product.Id, "New name", "New description", -5, "EUR");

        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProductErrors.PriceNegative);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
