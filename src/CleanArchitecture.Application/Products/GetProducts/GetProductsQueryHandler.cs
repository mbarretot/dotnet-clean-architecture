using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;
using Microsoft.Extensions.Caching.Hybrid;

namespace CleanArchitecture.Application.Products.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository productRepository, HybridCache cache)
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<Result<IReadOnlyList<ProductResponse>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var criteria = new ProductSearchCriteria(
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.MinPrice,
            request.MaxPrice,
            ProductSortFields.ToSortOrder(request.Sort),
            request.PageNumber,
            request.PageSize);

        var response = await cache.GetOrCreateAsync(
            ProductCacheKeys.List(request),
            (productRepository, criteria),
            static async (state, token) =>
            {
                var products = await state.productRepository
                    .SearchAsync(state.criteria, token)
                    .ConfigureAwait(false);

                return products.Select(product => product.ToResponse()).ToList();
            },
            tags: ProductCacheKeys.Tags,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<ProductResponse>>(response);
    }
}
