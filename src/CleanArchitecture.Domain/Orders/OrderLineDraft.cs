using CleanArchitecture.Domain.Products;

namespace CleanArchitecture.Domain.Orders;

public sealed record OrderLineDraft(Guid ProductId, string ProductName, Money UnitPrice, int Quantity);
