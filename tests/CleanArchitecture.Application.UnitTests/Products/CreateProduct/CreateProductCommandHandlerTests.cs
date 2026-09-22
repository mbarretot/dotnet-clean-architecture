using CleanArchitecture.Application.Products.CreateProduct;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Abstractions;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.CreateProduct;

public class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private CreateProductCommandHandler CreateSut() => new(_productRepository, _unitOfWork, _dateTimeProvider);

    private static CreateProductCommand ValidCommand() => new("Keyboard", "Mechanical keyboard", 49.99m, "USD", "SKU-1");

    [Fact]
    public async Task Handle_WithValidCommand_CreatesProductAndSavesChanges()
    {
        _productRepository.ExistsBySkuAsync("SKU-1", Arg.Any<CancellationToken>()).Returns(false);
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(now);
        var sut = CreateSut();

        var result = await sut.Handle(ValidCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        _productRepository.Received(1).Add(Arg.Is<Product>(product => product.Sku.Value == "SKU-1" && product.CreatedAt == now));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSkuAlreadyExists_ReturnsConflictWithoutSaving()
    {
        _productRepository.ExistsBySkuAsync("SKU-1", Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateSut();

        var result = await sut.Handle(ValidCommand(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProductErrors.SkuAlreadyExists);
        _productRepository.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNegativePrice_ReturnsFailureWithoutSaving()
    {
        _productRepository.ExistsBySkuAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut();
        var command = ValidCommand() with { Price = -1 };

        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProductErrors.PriceNegative);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBlankSku_ReturnsFailureWithoutCheckingExistence()
    {
        var sut = CreateSut();
        var command = ValidCommand() with { Sku = " " };

        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProductErrors.SkuRequired);
        await _productRepository.DidNotReceive().ExistsBySkuAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
