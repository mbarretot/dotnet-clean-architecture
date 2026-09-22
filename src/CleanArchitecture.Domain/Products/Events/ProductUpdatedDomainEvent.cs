using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Domain.Products.Events;

public sealed record ProductUpdatedDomainEvent(Guid ProductId) : IDomainEvent;
