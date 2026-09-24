using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Application.Orders.GetMyOrders;

public sealed class GetMyOrdersQueryHandler(IOrderRepository orderRepository, ICurrentUser currentUser)
    : IQueryHandler<GetMyOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
        {
            return Result.Success<IReadOnlyList<OrderResponse>>([]);
        }

        var orders = await orderRepository
            .GetByCustomerAsync(customerId, request.PageNumber, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<OrderResponse> response = orders.Select(order => order.ToResponse()).ToList();

        return Result.Success(response);
    }
}
