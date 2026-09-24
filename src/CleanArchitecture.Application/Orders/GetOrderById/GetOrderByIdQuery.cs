using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Orders.GetOrderById;

public sealed record GetOrderByIdQuery(Guid Id) : IQuery<OrderResponse>;
