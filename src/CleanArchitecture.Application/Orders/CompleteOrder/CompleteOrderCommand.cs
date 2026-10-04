using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Orders.CompleteOrder;

public sealed record CompleteOrderCommand(Guid Id) : ICommand;
