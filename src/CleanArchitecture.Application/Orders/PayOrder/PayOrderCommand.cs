using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Application.Orders.PayOrder;

public sealed record PayOrderCommand(Guid Id) : ICommand;
