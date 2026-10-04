using CleanArchitecture.Domain.Products;

namespace CleanArchitecture.Application.Products.GetProducts;

public static class ProductSortFields
{
    public const string Name = "name";

    public const string NameDescending = "-name";

    public const string Price = "price";

    public const string PriceDescending = "-price";

    public static readonly IReadOnlyList<string> All = [Name, NameDescending, Price, PriceDescending];

    public static bool IsValid(string? sort) =>
        string.IsNullOrWhiteSpace(sort) || All.Contains(sort.Trim(), StringComparer.OrdinalIgnoreCase);

    internal static ProductSortOrder ToSortOrder(string? sort) => sort?.Trim().ToLowerInvariant() switch
    {
        NameDescending => ProductSortOrder.NameDescending,
        Price => ProductSortOrder.PriceAscending,
        PriceDescending => ProductSortOrder.PriceDescending,
        _ => ProductSortOrder.NameAscending,
    };
}
