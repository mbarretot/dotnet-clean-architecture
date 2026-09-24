using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Domain.Products;

public static class ProductErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Product.NameRequired", "The product name is required.");

    public static readonly Error PriceNegative =
        Error.Validation("Product.PriceNegative", "The product price cannot be negative.");

    public static readonly Error CurrencyRequired =
        Error.Validation("Product.CurrencyRequired", "The product currency is required.");

    public static readonly Error SkuRequired =
        Error.Validation("Product.SkuRequired", "The product SKU is required.");

    public static readonly Error SkuAlreadyExists =
        Error.Conflict("Product.SkuAlreadyExists", "A product with the same SKU already exists.");

    public static Error NotFound(Guid productId) =>
        Error.NotFound("Product.NotFound", $"The product with identifier '{productId}' was not found.");
}
