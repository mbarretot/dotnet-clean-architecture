using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Domain.Products.Events;

public sealed record ProductDeletedDomainEvent(Guid ProductId) : IDomainEvent;
