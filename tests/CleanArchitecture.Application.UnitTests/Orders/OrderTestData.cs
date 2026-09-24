using CleanArchitecture.Domain.Orders;
using CleanArchitecture.Domain.Products;

namespace CleanArchitecture.Application.UnitTests.Orders;

internal static class OrderTestData
{
    public static Order PlacedOrder(string customerId, decimal unitPrice = 10m, int quantity = 1) =>
        Order.Place(customerId, [new OrderLineDraft(Guid.NewGuid(), "Keyboard", Money.Create(unitPrice, "USD").Value, quantity)]).Value;
}
