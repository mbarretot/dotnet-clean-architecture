using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Domain.Products.Events;

public sealed record ProductStockChangedDomainEvent(Guid ProductId, int StockQuantity) : IDomainEvent;
