using CleanArchitecture.Domain.Orders;

namespace CleanArchitecture.Application.Orders;

internal static class OrderMappingExtensions
{
    public static OrderResponse ToResponse(this Order order) =>
        new(
            order.Id,
            order.Status.ToString(),
            order.Total.Amount,
            order.Total.Currency,
            order.CreatedAt,
            order.Lines.Select(line => line.ToResponse()).ToList());

    private static OrderLineResponse ToResponse(this OrderLine line) =>
        new(line.ProductId, line.ProductName, line.UnitPrice.Amount, line.Quantity, line.LineTotal.Amount);
}
