using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Domain.Orders.Events;

public sealed record OrderShippedDomainEvent(Guid OrderId) : IDomainEvent;
