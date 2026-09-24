using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Orders.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (order is null || order.CustomerId != currentUser.UserId)
        {
            return Result.Failure(OrderErrors.NotFound(request.Id));
        }

        var cancelResult = order.Cancel();
        if (cancelResult.IsFailure)
        {
            return cancelResult;
        }

        order.ModifiedAt = dateTimeProvider.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
