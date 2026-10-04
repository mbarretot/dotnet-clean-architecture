using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Orders.PayOrder;

public sealed class PayOrderCommandHandler(
    IOrderRepository orderRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<PayOrderCommand>
{
    public async Task<Result> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (order is null || order.CustomerId != currentUser.UserId)
        {
            return Result.Failure(OrderErrors.NotFound(request.Id));
        }

        var payResult = order.Pay();
        if (payResult.IsFailure)
        {
            return payResult;
        }

        order.ModifiedAt = dateTimeProvider.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
