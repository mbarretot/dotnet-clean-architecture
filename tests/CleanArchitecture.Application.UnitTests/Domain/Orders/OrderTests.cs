using CleanArchitecture.Domain.Orders;
using CleanArchitecture.Domain.Orders.Events;
using CleanArchitecture.Domain.Products;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Domain.Orders;

/// <summary>The order owns its lines: every invariant is enforced by <see cref="Order.Place"/> and <see cref="Order.Cancel"/>.</summary>
public class OrderTests
{
    private const string CustomerId = "customer-1";

    private static readonly Guid KeyboardId = Guid.NewGuid();
    private static readonly Guid MouseId = Guid.NewGuid();

    private static OrderLineDraft Keyboard(int quantity = 1, string currency = "USD") =>
        new(KeyboardId, "Keyboard", Money.Create(50m, currency).Value, quantity);

    private static OrderLineDraft Mouse(int quantity = 1, string currency = "USD") =>
        new(MouseId, "Mouse", Money.Create(20m, currency).Value, quantity);

    private static Order PlaceOrder() => Order.Place(CustomerId, [Keyboard(2), Mouse()]).Value;

    [Fact]
    public void Place_WithValidLines_CreatesPlacedOrderWithSnapshotsAndTotal()
    {
        var result = Order.Place(CustomerId, [Keyboard(2), Mouse()]);

        result.IsSuccess.ShouldBeTrue();
        var order = result.Value;
        order.CustomerId.ShouldBe(CustomerId);
        order.Status.ShouldBe(OrderStatus.Placed);
        order.Lines.Count.ShouldBe(2);
        var keyboardLine = order.Lines.Single(line => line.ProductId == KeyboardId);
        keyboardLine.ProductName.ShouldBe("Keyboard");
        keyboardLine.UnitPrice.ShouldBe(Money.Create(50m, "USD").Value);
        keyboardLine.Quantity.ShouldBe(2);
        keyboardLine.LineTotal.ShouldBe(Money.Create(100m, "USD").Value);
        order.Total.ShouldBe(Money.Create(120m, "USD").Value);
    }

    [Fact]
    public void Place_RaisesOrderPlacedDomainEvent()
    {
        var order = PlaceOrder();

        order.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<OrderPlacedDomainEvent>()
            .OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public void Place_WithSameProductTwice_MergesQuantitiesIntoOneLine()
    {
        var order = Order.Place(CustomerId, [Keyboard(1), Mouse(), Keyboard(3)]).Value;

        order.Lines.Count.ShouldBe(2);
        order.Lines.Single(line => line.ProductId == KeyboardId).Quantity.ShouldBe(4);
        order.Total.ShouldBe(Money.Create(220m, "USD").Value);
    }

    [Fact]
    public void Place_WithoutLines_Fails()
    {
        var result = Order.Place(CustomerId, []);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.NoLines);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Place_WithNonPositiveQuantity_Fails(int quantity)
    {
        var result = Order.Place(CustomerId, [Keyboard(quantity)]);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.QuantityNotPositive);
    }

    [Fact]
    public void Place_WithLinesInDifferentCurrencies_Fails()
    {
        var result = Order.Place(CustomerId, [Keyboard(currency: "USD"), Mouse(currency: "EUR")]);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.CurrencyMismatch);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Place_WithoutCustomer_Fails(string customerId)
    {
        var result = Order.Place(customerId, [Keyboard()]);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.CustomerRequired);
    }

    [Fact]
    public void Cancel_PlacedOrder_MarksItCancelledAndRaisesOrderCancelledDomainEvent()
    {
        var order = PlaceOrder();
        order.ClearDomainEvents();

        var result = order.Cancel();

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<OrderCancelledDomainEvent>()
            .OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public void Cancel_AlreadyCancelledOrder_FailsWithoutRaisingAnotherEvent()
    {
        var order = PlaceOrder();
        order.Cancel();
        order.ClearDomainEvents();

        var result = order.Cancel();

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrderErrors.AlreadyCancelled);
        order.DomainEvents.ShouldBeEmpty();
    }
}
