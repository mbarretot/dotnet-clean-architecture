using CleanArchitecture.Application.Products.SetProductStock;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.Domain.Products.Events;
using CleanArchitecture.SharedKernel.Abstractions;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Products.SetProductStock;

public class SetProductStockCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private SetProductStockCommandHandler CreateSut() => new(_productRepository, _unitOfWork, _dateTimeProvider);

    [Fact]
    public async Task Handle_WithExistingProduct_SetsStockRaisesEventAndSaves()
    {
        var product = Product.Create("Mouse", "d", Money.Create(5m, "USD").Value, Sku.Create("SKU-1").Value).Value;
        product.ClearDomainEvents();
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await CreateSut().Handle(new SetProductStockCommand(product.Id, 12), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        product.StockQuantity.ShouldBe(12);
        product.DomainEvents.ShouldHaveSingleItem().ShouldBe(new ProductStockChangedDomainEvent(product.Id, 12));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownProduct_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _productRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await CreateSut().Handle(new SetProductStockCommand(id, 1), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(ProductErrors.NotFound(id));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(10, true)]
    [InlineData(-1, false)]
    public void Validator_AcceptsOnlyNonNegativeStock(int stock, bool expected) =>
        new SetProductStockCommandValidator().Validate(new SetProductStockCommand(Guid.NewGuid(), stock)).IsValid.ShouldBe(expected);

    [Fact]
    public void Validator_RequiresAnId() =>
        new SetProductStockCommandValidator().Validate(new SetProductStockCommand(Guid.Empty, 1)).IsValid.ShouldBeFalse();
}
