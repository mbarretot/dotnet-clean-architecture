using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Products.SetProductStock;

public sealed class SetProductStockCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<SetProductStockCommand>
{
    public async Task<Result> Handle(SetProductStockCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(request.Id));
        }

        var stockResult = product.SetStock(request.StockQuantity);
        if (stockResult.IsFailure)
        {
            return stockResult;
        }

        product.ModifiedAt = dateTimeProvider.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
