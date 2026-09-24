using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Domain.Orders.Events;

public sealed record OrderPlacedDomainEvent(Guid OrderId) : IDomainEvent;
