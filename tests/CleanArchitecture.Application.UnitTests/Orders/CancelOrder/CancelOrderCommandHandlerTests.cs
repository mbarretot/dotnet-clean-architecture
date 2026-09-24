using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Application.Orders.CancelOrder;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.SharedKernel.Abstractions;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Orders.CancelOrder;

public class CancelOrderCommandHandlerTests
{
    private const string CustomerId = "customer-1";

    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CancelOrderCommandHandlerTests() => _currentUser.UserId.Returns(CustomerId);

    private CancelOrderCommandHandler CreateSut() => new(_orderRepository, _currentUser, _unitOfWork, _dateTimeProvider);

    private Order GivenOrder(string customerId)
    {
        var order = OrderTestData.PlacedOrder(customerId);
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        return order;
    }

    [Fact]
    public async Task Handle_WithOwnPlacedOrder_CancelsAndSaves()
    {
        var order = GivenOrder(CustomerId);
        var now = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(now);

        var result = await CreateSut().Handle(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.ModifiedAt.ShouldBe(now);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAlreadyCancelledOrder_ReturnsConflictWithoutSaving()
    {
        var order = GivenOrder(CustomerId);
        order.Cancel();

        var result = await CreateSut().Handle(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.AlreadyCancelled);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnotherCustomersOrder_ReturnsNotFoundAndLeavesItPlaced()
    {
        var order = GivenOrder("someone-else");

        var result = await CreateSut().Handle(new CancelOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.NotFound(order.Id));
        order.Status.ShouldBe(OrderStatus.Placed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownOrder_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _orderRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var result = await CreateSut().Handle(new CancelOrderCommand(id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.NotFound(id));
    }
}
