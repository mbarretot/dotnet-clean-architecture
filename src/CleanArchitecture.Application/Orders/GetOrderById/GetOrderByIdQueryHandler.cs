using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Orders.GetOrderById;

/// <summary>Another customer's order reads as not found, so its existence is never revealed.</summary>
public sealed class GetOrderByIdQueryHandler(IOrderRepository orderRepository, ICurrentUser currentUser)
    : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        return order is null || order.CustomerId != currentUser.UserId
            ? Result.Failure<OrderResponse>(OrderErrors.NotFound(request.Id))
            : order.ToResponse();
    }
}
