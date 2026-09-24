using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Orders.PlaceOrder;

public sealed record PlaceOrderCommand(IReadOnlyList<PlaceOrderLine> Lines) : ICommand<Guid>;

public sealed record PlaceOrderLine(Guid ProductId, int Quantity);
