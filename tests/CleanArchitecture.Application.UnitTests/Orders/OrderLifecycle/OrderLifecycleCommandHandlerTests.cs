using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Application.Orders.CompleteOrder;
using CleanArchitecture.Application.Orders.PayOrder;
using CleanArchitecture.Application.Orders.ShipOrder;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.SharedKernel.Abstractions;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Orders.OrderLifecycle;

public class OrderLifecycleCommandHandlerTests
{
    private const string CustomerId = "customer-1";

    private static readonly DateTimeOffset Now = new(2026, 3, 4, 0, 0, 0, TimeSpan.Zero);

    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public OrderLifecycleCommandHandlerTests()
    {
        _currentUser.UserId.Returns(CustomerId);
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private Order GivenOrder(string customerId = CustomerId)
    {
        var order = OrderTestData.PlacedOrder(customerId);
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        return order;
    }

    private Task<SharedKernel.Results.Result> PayAsync(Guid id) =>
        new PayOrderCommandHandler(_orderRepository, _currentUser, _unitOfWork, _dateTimeProvider)
            .Handle(new PayOrderCommand(id), TestContext.Current.CancellationToken);

    private Task<SharedKernel.Results.Result> ShipAsync(Guid id) =>
        new ShipOrderCommandHandler(_orderRepository, _unitOfWork, _dateTimeProvider)
            .Handle(new ShipOrderCommand(id), TestContext.Current.CancellationToken);

    private Task<SharedKernel.Results.Result> CompleteAsync(Guid id) =>
        new CompleteOrderCommandHandler(_orderRepository, _unitOfWork, _dateTimeProvider)
            .Handle(new CompleteOrderCommand(id), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Pay_Ship_Complete_WalkTheOrderThroughItsLifecycle()
    {
        var order = GivenOrder();

        (await PayAsync(order.Id)).IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Paid);

        (await ShipAsync(order.Id)).IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Shipped);

        (await CompleteAsync(order.Id)).IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Completed);
        order.ModifiedAt.ShouldBe(Now);

        await _unitOfWork.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pay_WithAnotherCustomersOrder_ReturnsNotFound()
    {
        var order = GivenOrder("someone-else");

        var result = await PayAsync(order.Id);

        result.Error.ShouldBe(OrderErrors.NotFound(order.Id));
        order.Status.ShouldBe(OrderStatus.Placed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ship_DoesNotRequireTheOrderToBelongToTheCaller()
    {
        var order = GivenOrder("someone-else");
        order.Pay();

        var result = await ShipAsync(order.Id);

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Shipped);
    }

    [Fact]
    public async Task Ship_WithUnpaidOrder_ReturnsConflictWithoutSaving()
    {
        var order = GivenOrder();

        var result = await ShipAsync(order.Id);

        result.Error.ShouldBe(OrderErrors.InvalidStatusTransition(OrderStatus.Placed, OrderStatus.Shipped));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_WithPaidButUnshippedOrder_ReturnsConflict()
    {
        var order = GivenOrder();
        order.Pay();

        var result = await CompleteAsync(order.Id);

        result.Error.ShouldBe(OrderErrors.InvalidStatusTransition(OrderStatus.Paid, OrderStatus.Completed));
    }

    [Fact]
    public async Task Handlers_WithUnknownOrder_ReturnNotFound()
    {
        var id = Guid.NewGuid();
        _orderRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Order?)null);

        (await PayAsync(id)).Error.ShouldBe(OrderErrors.NotFound(id));
        (await ShipAsync(id)).Error.ShouldBe(OrderErrors.NotFound(id));
        (await CompleteAsync(id)).Error.ShouldBe(OrderErrors.NotFound(id));
    }
}
