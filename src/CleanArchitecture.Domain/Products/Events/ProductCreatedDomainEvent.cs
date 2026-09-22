using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Domain.Products.Events;

public sealed record ProductCreatedDomainEvent(Guid ProductId) : IDomainEvent;
