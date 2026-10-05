using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Domain.Orders.Events;

public sealed record OrderCompletedDomainEvent(Guid OrderId) : IDomainEvent;
