using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.Domain.Products.Events;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Infrastructure.UnitTests.Persistence.Interceptors;

public sealed class DispatchDomainEventsInterceptorTests : IDisposable
{
    private readonly SqliteApplicationDbContextFixture _fixture = new();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public void Dispose() => _fixture.Dispose();

    private static Product NewProduct(string sku) =>
        Product.Create("Keyboard", "Mechanical keyboard", Money.Create(49.99m, "USD").Value, Sku.Create(sku).Value).Value;

    [Fact]
    public async Task SaveChangesAsync_WhenAggregateRaisesDomainEvent_PublishesItAndClearsIt()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.UserId.Returns("user-123");

        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var product = NewProduct("SKU-EVT-1");
        product.DomainEvents.ShouldHaveSingleItem();

        context.Products.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _publisher.Received(1).Publish(
            Arg.Is<ProductCreatedDomainEvent>(domainEvent => domainEvent.ProductId == product.Id),
            Arg.Any<CancellationToken>());
        product.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoDomainEventsAreRaised_DoesNotPublishAnything()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.UserId.Returns("user-123");

        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var product = NewProduct("SKU-EVT-2");
        context.Products.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _publisher.ClearReceivedCalls();

        context.Products.Update(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _publisher.ReceivedCalls().ShouldBeEmpty();
    }
}
