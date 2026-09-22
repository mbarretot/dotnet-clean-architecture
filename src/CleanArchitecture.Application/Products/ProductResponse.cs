namespace CleanArchitecture.Application.Products;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    bool IsActive);
