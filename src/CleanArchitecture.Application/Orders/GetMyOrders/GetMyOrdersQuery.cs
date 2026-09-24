using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Orders.GetMyOrders;

public sealed record GetMyOrdersQuery(int PageNumber = 1, int PageSize = 20) : IQuery<IReadOnlyList<OrderResponse>>;
