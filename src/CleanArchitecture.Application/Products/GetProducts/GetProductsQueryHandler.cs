using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Products.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<Result<IReadOnlyList<ProductResponse>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository
            .GetAllAsync(request.PageNumber, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ProductResponse> response = products.Select(product => product.ToResponse()).ToList();

        return Result.Success(response);
    }
}
