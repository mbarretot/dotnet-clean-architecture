using CleanArchitecture.Domain.Products.Events;
using CleanArchitecture.SharedKernel.Messaging;
using Microsoft.Extensions.Caching.Hybrid;

namespace CleanArchitecture.Application.Products.EventHandlers;

/// <summary>Drops every cached product read when any product changes; events are dispatched after the commit.</summary>
public sealed class ProductCacheInvalidationHandler(HybridCache cache) :
    INotificationHandler<ProductCreatedDomainEvent>,
    INotificationHandler<ProductUpdatedDomainEvent>,
    INotificationHandler<ProductDeletedDomainEvent>,
    INotificationHandler<ProductStockChangedDomainEvent>
{
    public Task Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(cancellationToken);

    public Task Handle(ProductUpdatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(cancellationToken);

    public Task Handle(ProductDeletedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(cancellationToken);

    public Task Handle(ProductStockChangedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(cancellationToken);

    private Task InvalidateAsync(CancellationToken cancellationToken) =>
        cache.RemoveByTagAsync(ProductCacheKeys.Tag, cancellationToken).AsTask();
}
