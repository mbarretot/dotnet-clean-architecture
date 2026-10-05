using CleanArchitecture.Domain.Orders;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Orders.CompleteOrder;

public sealed class CompleteOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CompleteOrderCommand>
{
    public async Task<Result> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound(request.Id));
        }

        var transitionResult = order.Complete();
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        order.ModifiedAt = dateTimeProvider.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
