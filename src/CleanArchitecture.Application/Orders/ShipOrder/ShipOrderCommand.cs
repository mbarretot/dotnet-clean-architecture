using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Orders.ShipOrder;

public sealed record ShipOrderCommand(Guid Id) : ICommand;
