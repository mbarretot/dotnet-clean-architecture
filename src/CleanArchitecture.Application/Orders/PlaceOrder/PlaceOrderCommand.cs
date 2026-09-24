using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Orders.PlaceOrder;

/// <summary>The customer is not part of the command: it is always the current user.</summary>
public sealed record PlaceOrderCommand(IReadOnlyList<PlaceOrderLine> Lines) : ICommand<Guid>;

public sealed record PlaceOrderLine(Guid ProductId, int Quantity);
