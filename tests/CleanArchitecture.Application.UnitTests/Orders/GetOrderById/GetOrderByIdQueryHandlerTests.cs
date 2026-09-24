using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Application.Orders.GetOrderById;
using CleanArchitecture.Domain.Orders;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Orders.GetOrderById;

public class GetOrderByIdQueryHandlerTests
{
    private const string CustomerId = "customer-1";

    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public GetOrderByIdQueryHandlerTests() => _currentUser.UserId.Returns(CustomerId);

    private GetOrderByIdQueryHandler CreateSut() => new(_orderRepository, _currentUser);

    [Fact]
    public async Task Handle_WithOwnOrder_ReturnsMappedOrder()
    {
        var order = OrderTestData.PlacedOrder(CustomerId, unitPrice: 25m, quantity: 2);
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await CreateSut().Handle(new GetOrderByIdQuery(order.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(order.Id);
        result.Value.Status.ShouldBe("Placed");
        result.Value.Total.ShouldBe(50m);
        result.Value.Currency.ShouldBe("USD");
        var line = result.Value.Lines.ShouldHaveSingleItem();
        line.ProductName.ShouldBe("Keyboard");
        line.UnitPrice.ShouldBe(25m);
        line.Quantity.ShouldBe(2);
        line.LineTotal.ShouldBe(50m);
    }

    [Fact]
    public async Task Handle_WithAnotherCustomersOrder_ReturnsNotFound()
    {
        var order = OrderTestData.PlacedOrder("someone-else");
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await CreateSut().Handle(new GetOrderByIdQuery(order.Id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.NotFound(order.Id));
    }

    [Fact]
    public async Task Handle_WithUnknownOrder_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _orderRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var result = await CreateSut().Handle(new GetOrderByIdQuery(id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.NotFound(id));
    }
}
