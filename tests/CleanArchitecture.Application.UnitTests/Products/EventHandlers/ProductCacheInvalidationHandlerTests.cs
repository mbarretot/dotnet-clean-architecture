using CleanArchitecture.Application.Products.EventHandlers;
using CleanArchitecture.Domain.Products.Events;
using CleanArchitecture.SharedKernel.Messaging;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace CleanArchitecture.Application.UnitTests.Products.EventHandlers;

public class ProductCacheInvalidationHandlerTests
{
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    public static TheoryData<IDomainEvent> ProductEvents =>
    [
        new ProductCreatedDomainEvent(Guid.NewGuid()),
        new ProductUpdatedDomainEvent(Guid.NewGuid()),
        new ProductDeletedDomainEvent(Guid.NewGuid()),
        new ProductStockChangedDomainEvent(Guid.NewGuid(), 3),
    ];

    [Theory]
    [MemberData(nameof(ProductEvents))]
    public async Task Handle_AnyProductEvent_InvalidatesTheProductsTag(IDomainEvent domainEvent)
    {
        var sut = new ProductCacheInvalidationHandler(_cache);
        var cancellationToken = TestContext.Current.CancellationToken;

        await (domainEvent switch
        {
            ProductCreatedDomainEvent created => sut.Handle(created, cancellationToken),
            ProductUpdatedDomainEvent updated => sut.Handle(updated, cancellationToken),
            ProductDeletedDomainEvent deleted => sut.Handle(deleted, cancellationToken),
            ProductStockChangedDomainEvent stockChanged => sut.Handle(stockChanged, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(domainEvent)),
        });

        await _cache.Received(1).RemoveByTagAsync("products", cancellationToken);
    }
}
