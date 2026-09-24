using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Application.Orders.PlaceOrder;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Abstractions;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Orders.PlaceOrder;

public class PlaceOrderCommandHandlerTests
{
    private const string CustomerId = "customer-1";

    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public PlaceOrderCommandHandlerTests() => _currentUser.UserId.Returns(CustomerId);

    private PlaceOrderCommandHandler CreateSut() =>
        new(_orderRepository, _productRepository, _currentUser, _unitOfWork, _dateTimeProvider);

    private Product GivenProduct(string name, decimal price, string currency = "USD")
    {
        var product = Product.Create(name, "Description", Money.Create(price, currency).Value, Sku.Create($"SKU-{name}").Value).Value;
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        return product;
    }

    [Fact]
    public async Task Handle_WithExistingProducts_PlacesOrderWithSnapshotsForCurrentUserAndSavesOnce()
    {
        var keyboard = GivenProduct("Keyboard", 50m);
        var mouse = GivenProduct("Mouse", 20m);
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(now);
        var command = new PlaceOrderCommand([new PlaceOrderLine(keyboard.Id, 2), new PlaceOrderLine(mouse.Id, 1)]);

        var result = await CreateSut().Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _orderRepository.Received(1).Add(Arg.Is<Order>(order =>
            order.Id == result.Value
            && order.CustomerId == CustomerId
            && order.CreatedAt == now
            && order.Total == Money.Create(120m, "USD").Value
            && order.Lines.Any(line => line.ProductId == keyboard.Id && line.ProductName == "Keyboard" && line.Quantity == 2)));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTheSameProductTwice_LoadsItOnceAndMergesTheLines()
    {
        var keyboard = GivenProduct("Keyboard", 50m);
        var command = new PlaceOrderCommand([new PlaceOrderLine(keyboard.Id, 1), new PlaceOrderLine(keyboard.Id, 2)]);

        var result = await CreateSut().Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        await _productRepository.Received(1).GetByIdAsync(keyboard.Id, Arg.Any<CancellationToken>());
        _orderRepository.Received(1).Add(Arg.Is<Order>(order => order.Lines.Single().Quantity == 3));
    }

    [Fact]
    public async Task Handle_WithUnknownOrDeletedProduct_ReturnsProductNotFoundWithoutSaving()
    {
        var keyboard = GivenProduct("Keyboard", 50m);
        var missingId = Guid.NewGuid();
        _productRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var command = new PlaceOrderCommand([new PlaceOrderLine(keyboard.Id, 1), new PlaceOrderLine(missingId, 1)]);

        var result = await CreateSut().Handle(command, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.ProductNotFound(missingId));
        _orderRepository.DidNotReceive().Add(Arg.Any<Order>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProductsInDifferentCurrencies_ReturnsCurrencyMismatchWithoutSaving()
    {
        var keyboard = GivenProduct("Keyboard", 50m, "USD");
        var mouse = GivenProduct("Mouse", 20m, "EUR");
        var command = new PlaceOrderCommand([new PlaceOrderLine(keyboard.Id, 1), new PlaceOrderLine(mouse.Id, 1)]);

        var result = await CreateSut().Handle(command, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.CurrencyMismatch);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutCurrentUser_ReturnsCustomerRequiredWithoutLoadingProducts()
    {
        _currentUser.UserId.Returns((string?)null);
        var command = new PlaceOrderCommand([new PlaceOrderLine(Guid.NewGuid(), 1)]);

        var result = await CreateSut().Handle(command, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.CustomerRequired);
        await _productRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
