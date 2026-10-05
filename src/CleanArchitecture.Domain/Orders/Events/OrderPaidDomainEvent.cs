using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Domain.Orders.Events;

public sealed record OrderPaidDomainEvent(Guid OrderId) : IDomainEvent;
