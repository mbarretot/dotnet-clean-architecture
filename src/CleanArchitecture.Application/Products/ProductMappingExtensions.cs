using CleanArchitecture.Domain.Products;

namespace CleanArchitecture.Application.Products;

internal static class ProductMappingExtensions
{
    public static ProductResponse ToResponse(this Product product) =>
        new(product.Id, product.Name, product.Description, product.Price.Amount, product.Price.Currency, product.Sku.Value, product.IsActive);
}
