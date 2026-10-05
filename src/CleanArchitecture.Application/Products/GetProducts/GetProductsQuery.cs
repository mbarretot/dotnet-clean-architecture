using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Products.GetProducts;

/// <summary><see cref="Sort"/> is one of <see cref="ProductSortFields.All"/>; a leading <c>-</c> sorts descending.</summary>
public sealed record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Sort = null) : IQuery<IReadOnlyList<ProductResponse>>;
