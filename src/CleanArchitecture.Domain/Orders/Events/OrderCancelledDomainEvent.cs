using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Domain.Orders.Events;

public sealed record OrderCancelledDomainEvent(Guid OrderId) : IDomainEvent;
