using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Orders;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.Infrastructure.Persistence.Repositories;
using CleanArchitecture.Infrastructure.UnitTests.Persistence.Interceptors;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Infrastructure.UnitTests.Persistence.Repositories;

/// <summary>Proves the whole aggregate (lines and their owned <see cref="Money"/> snapshots) round-trips through real SQL.</summary>
public sealed class OrderRepositoryTests : IDisposable
{
    private readonly SqliteApplicationDbContextFixture _fixture = new();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetByIdAsync_ReturnsPersistedOrderWithLinesAndSnapshotsIntact()
    {
        var productId = Guid.NewGuid();
        var order = NewOrder("customer-1", new OrderLineDraft(productId, "Keyboard", Money.Create(49.99m, "USD").Value, 2));
        using (var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher))
        {
            new OrderRepository(context).Add(order);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var readContext = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var loaded = await new OrderRepository(readContext).GetByIdAsync(order.Id, TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.CustomerId.ShouldBe("customer-1");
        loaded.Status.ShouldBe(OrderStatus.Placed);
        var line = loaded.Lines.ShouldHaveSingleItem();
        line.ProductId.ShouldBe(productId);
        line.ProductName.ShouldBe("Keyboard");
        line.UnitPrice.ShouldBe(Money.Create(49.99m, "USD").Value);
        line.Quantity.ShouldBe(2);
        loaded.Total.ShouldBe(Money.Create(99.98m, "USD").Value);
    }

    [Fact]
    public async Task Cancel_OnLoadedOrder_IsPersistedBySavingTheTrackedAggregate()
    {
        var order = NewOrder("customer-1", Draft());
        using (var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher))
        {
            new OrderRepository(context).Add(order);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using (var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher))
        {
            var loaded = await new OrderRepository(context).GetByIdAsync(order.Id, TestContext.Current.CancellationToken);
            loaded.ShouldNotBeNull().Cancel().IsSuccess.ShouldBeTrue();
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var readContext = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var reloaded = await new OrderRepository(readContext).GetByIdAsync(order.Id, TestContext.Current.CancellationToken);
        reloaded.ShouldNotBeNull().Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task GetByCustomerAsync_ReturnsOnlyThatCustomersOrdersNewestFirstAndPaged()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var oldest = NewOrder("customer-1", Draft());
        var middle = NewOrder("customer-1", Draft());
        var newest = NewOrder("customer-1", Draft());
        var someoneElses = NewOrder("customer-2", Draft());
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = new OrderRepository(context);
        var offset = 0;
        foreach (var order in new[] { oldest, middle, newest, someoneElses })
        {
            _dateTimeProvider.UtcNow.Returns(baseTime.AddMinutes(offset++));
            repository.Add(order);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        context.ChangeTracker.Clear();

        var firstPage = await repository.GetByCustomerAsync("customer-1", 1, 2, TestContext.Current.CancellationToken);
        var secondPage = await repository.GetByCustomerAsync("customer-1", 2, 2, TestContext.Current.CancellationToken);

        firstPage.Select(order => order.Id).ShouldBe([newest.Id, middle.Id]);
        secondPage.Select(order => order.Id).ShouldBe([oldest.Id]);
        firstPage.ShouldAllBe(order => order.Lines.Count == 1);
    }

    private static OrderLineDraft Draft() => new(Guid.NewGuid(), "Mouse", Money.Create(10m, "USD").Value, 1);

    private static Order NewOrder(string customerId, OrderLineDraft line) => Order.Place(customerId, [line]).Value;
}
