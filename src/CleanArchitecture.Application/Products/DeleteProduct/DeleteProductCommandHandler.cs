using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Products.DeleteProduct;

/// <summary>
/// Soft-deletes to preserve history and referential integrity for past orders. Deleted products are hidden
/// by a global query filter, so deleting one twice finds nothing and returns not found.
/// </summary>
public sealed class DeleteProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(request.Id));
        }

        product.Delete();

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
