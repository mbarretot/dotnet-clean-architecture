using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Orders.CancelOrder;

public sealed record CancelOrderCommand(Guid Id) : ICommand;
