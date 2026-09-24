using CleanArchitecture.Domain.Products;

namespace CleanArchitecture.Domain.Orders;

/// <summary>
/// A line as requested when placing an order. <paramref name="ProductName"/> and <paramref name="UnitPrice"/> are
/// snapshots of the product at order time, so later catalog changes never rewrite an existing order.
/// </summary>
public sealed record OrderLineDraft(Guid ProductId, string ProductName, Money UnitPrice, int Quantity);
