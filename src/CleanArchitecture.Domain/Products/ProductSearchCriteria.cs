namespace CleanArchitecture.Domain.Products;

/// <summary><see cref="Search"/> is matched case-insensitively against the name and description; null matches all.</summary>
public sealed record ProductSearchCriteria(
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    ProductSortOrder SortOrder = ProductSortOrder.NameAscending,
    int PageNumber = 1,
    int PageSize = 20);
