namespace CleanArchitecture.Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    string Status,
    decimal Total,
    string Currency,
    DateTimeOffset PlacedAt,
    IReadOnlyList<OrderLineResponse> Lines);

/// <summary><see cref="ProductName"/> and <see cref="UnitPrice"/> are snapshots taken when the order was placed.</summary>
public sealed record OrderLineResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
