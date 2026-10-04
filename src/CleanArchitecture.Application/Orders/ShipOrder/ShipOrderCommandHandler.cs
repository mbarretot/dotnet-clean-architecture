using CleanArchitecture.Domain.Orders;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Orders.ShipOrder;

public sealed class ShipOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ShipOrderCommand>
{
    public async Task<Result> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound(request.Id));
        }

        var transitionResult = order.Ship();
        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        order.ModifiedAt = dateTimeProvider.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
