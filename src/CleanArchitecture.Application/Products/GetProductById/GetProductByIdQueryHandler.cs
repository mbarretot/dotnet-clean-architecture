using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;
using Microsoft.Extensions.Caching.Hybrid;

namespace CleanArchitecture.Application.Products.GetProductById;

public sealed class GetProductByIdQueryHandler(IProductRepository productRepository, HybridCache cache)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await cache.GetOrCreateAsync(
            ProductCacheKeys.ById(request.Id),
            (productRepository, request.Id),
            static async (state, token) =>
                (await state.productRepository.GetByIdAsync(state.Id, token).ConfigureAwait(false))?.ToResponse(),
            tags: ProductCacheKeys.Tags,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return product is null
            ? Result.Failure<ProductResponse>(ProductErrors.NotFound(request.Id))
            : product;
    }
}
