using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Products.GetProductById;

public sealed class GetProductByIdQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        return product is null
            ? Result.Failure<ProductResponse>(ProductErrors.NotFound(request.Id))
            : product.ToResponse();
    }
}
