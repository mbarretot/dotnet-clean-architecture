using CleanArchitecture.Domain.Products.Events;
using CleanArchitecture.SharedKernel.Messaging;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Products.EventHandlers;

/// <summary>Sample handler; a real one might send a notification or update a read model.</summary>
public sealed class ProductCreatedDomainEventHandler(ILogger<ProductCreatedDomainEventHandler> logger)
    : DomainEventHandler<ProductCreatedDomainEvent>
{
    public override Task Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        ProductCreatedDomainEventHandlerMessages.ProductCreated(logger, notification.ProductId);
        return Task.CompletedTask;
    }
}

internal static partial class ProductCreatedDomainEventHandlerMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Product {ProductId} was created.")]
    public static partial void ProductCreated(ILogger logger, Guid productId);
}
