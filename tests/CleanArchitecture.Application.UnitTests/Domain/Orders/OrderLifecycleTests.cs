using CleanArchitecture.Domain.Orders;
using CleanArchitecture.Domain.Orders.Events;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Results;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Domain.Orders;

public class OrderLifecycleTests
{
    private static Order PlacedOrder()
    {
        var order = Order.Place(
            "customer-1", [new OrderLineDraft(Guid.NewGuid(), "Keyboard", Money.Create(50m, "USD").Value, 1)]).Value;
        order.ClearDomainEvents();

        return order;
    }

    private static Order OrderIn(OrderStatus status)
    {
        var order = PlacedOrder();
        Func<Result>[] path = status switch
        {
            OrderStatus.Placed => [],
            OrderStatus.Paid => [order.Pay],
            OrderStatus.Shipped => [order.Pay, order.Ship],
            OrderStatus.Completed => [order.Pay, order.Ship, order.Complete],
            OrderStatus.Cancelled => [order.Cancel],
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        foreach (var step in path)
        {
            step().IsSuccess.ShouldBeTrue();
        }

        order.ClearDomainEvents();

        return order;
    }

    [Fact]
    public void Pay_FromPlaced_MovesToPaidAndRaisesOrderPaid()
    {
        var order = PlacedOrder();

        order.Pay().IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Paid);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBe(new OrderPaidDomainEvent(order.Id));
    }

    [Fact]
    public void Ship_FromPaid_MovesToShippedAndRaisesOrderShipped()
    {
        var order = OrderIn(OrderStatus.Paid);

        order.Ship().IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Shipped);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBe(new OrderShippedDomainEvent(order.Id));
    }

    [Fact]
    public void Complete_FromShipped_MovesToCompletedAndRaisesOrderCompleted()
    {
        var order = OrderIn(OrderStatus.Shipped);

        order.Complete().IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Completed);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBe(new OrderCompletedDomainEvent(order.Id));
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.Paid)]
    public void Cancel_FromPlacedOrPaid_MovesToCancelledAndRaisesOrderCancelled(OrderStatus from)
    {
        var order = OrderIn(from);

        order.Cancel().IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBe(new OrderCancelledDomainEvent(order.Id));
    }

    [Theory]
    [InlineData(OrderStatus.Paid, OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Paid)]
    [InlineData(OrderStatus.Completed, OrderStatus.Paid)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid)]
    [InlineData(OrderStatus.Placed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Placed, OrderStatus.Completed)]
    [InlineData(OrderStatus.Paid, OrderStatus.Completed)]
    [InlineData(OrderStatus.Completed, OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Completed)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Completed, OrderStatus.Cancelled)]
    public void InvalidTransition_FailsWithConflictAndLeavesTheOrderUnchanged(OrderStatus from, OrderStatus to)
    {
        var order = OrderIn(from);

        var result = to switch
        {
            OrderStatus.Paid => order.Pay(),
            OrderStatus.Shipped => order.Ship(),
            OrderStatus.Completed => order.Complete(),
            OrderStatus.Cancelled => order.Cancel(),
            _ => throw new ArgumentOutOfRangeException(nameof(to)),
        };

        result.Error.ShouldBe(OrderErrors.InvalidStatusTransition(from, to));
        result.Error.ErrorType.ShouldBe(ErrorType.Conflict);
        order.Status.ShouldBe(from);
        order.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_KeepsTheDedicatedError()
    {
        var order = OrderIn(OrderStatus.Cancelled);

        order.Cancel().Error.ShouldBe(OrderErrors.AlreadyCancelled);
    }
}
