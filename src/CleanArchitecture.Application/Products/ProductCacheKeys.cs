using System.Globalization;
using CleanArchitecture.Application.Products.GetProducts;

namespace CleanArchitecture.Application.Products;

internal static class ProductCacheKeys
{
    /// <summary>Every cached product entry carries this tag, so one product change invalidates them all.</summary>
    public const string Tag = "products";

    public static readonly string[] Tags = [Tag];

    public static string ById(Guid id) => $"products:id:{id:N}";

    public static string List(GetProductsQuery query) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"products:list:{query.PageNumber}:{query.PageSize}:{query.Sort}:{query.MinPrice}:{query.MaxPrice}:{query.Search?.Trim().ToUpperInvariant()}");
}
