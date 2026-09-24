using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.Infrastructure.Persistence.Interceptors;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Infrastructure.UnitTests.Persistence.Interceptors;

public sealed class AuditableEntitySaveChangesInterceptorTests : IDisposable
{
    private readonly SqliteApplicationDbContextFixture _fixture = new();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public void Dispose() => _fixture.Dispose();

    private static Product NewProduct() =>
        Product.Create("Keyboard", "Mechanical keyboard", Money.Create(49.99m, "USD").Value, Sku.Create("SKU-AUDIT-1").Value).Value;

    [Fact]
    public async Task SaveChangesAsync_OnAdd_StampsCreatedByAndCreatedAt()
    {
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(now);
        _currentUser.UserId.Returns("user-123");

        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var product = NewProduct();
        context.Products.Add(product);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.CreatedBy.ShouldBe("user-123");
        product.CreatedAt.ShouldBe(now);
        product.ModifiedBy.ShouldBeNull();
        product.ModifiedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenUserIsAnonymous_FallsBackToSystemUser()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.UserId.Returns((string?)null);

        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var product = NewProduct();
        context.Products.Add(product);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.CreatedBy.ShouldBe(AuditableEntitySaveChangesInterceptor.SystemUser);
    }

    [Fact]
    public async Task SaveChangesAsync_OnUpdate_StampsModifiedByAndModifiedAtButLeavesCreatedAtUntouched()
    {
        var createdAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var modifiedAt = new DateTimeOffset(2026, 2, 1, 8, 30, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(createdAt);
        _currentUser.UserId.Returns("creator");

        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var product = NewProduct();
        context.Products.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var createdAtAfterInsert = product.CreatedAt;

        _dateTimeProvider.UtcNow.Returns(modifiedAt);
        _currentUser.UserId.Returns("editor");
        product.Update("Keyboard v2", "Updated description", Money.Create(59.99m, "USD").Value);
        context.Products.Update(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.CreatedAt.ShouldBe(createdAtAfterInsert);
        product.CreatedBy.ShouldBe("creator");
        product.ModifiedBy.ShouldBe("editor");
        product.ModifiedAt.ShouldBe(modifiedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_OnSoftDelete_StampsDeletedOnUtcAndDeletedBy()
    {
        var deletedAt = new DateTimeOffset(2026, 3, 1, 9, 15, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _currentUser.UserId.Returns("creator");

        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var product = NewProduct();
        context.Products.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dateTimeProvider.UtcNow.Returns(deletedAt);
        _currentUser.UserId.Returns("deleter");
        product.Delete();
        context.Products.Update(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.DeletedOnUtc.ShouldBe(deletedAt);
        product.DeletedBy.ShouldBe("deleter");
    }

    [Fact]
    public async Task SaveChangesAsync_OnSoftDeleteByAnonymousCaller_FallsBackToSystemUser()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.UserId.Returns((string?)null);

        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var product = NewProduct();
        context.Products.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.Delete();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.DeletedBy.ShouldBe(AuditableEntitySaveChangesInterceptor.SystemUser);
    }

    [Fact]
    public async Task SaveChangesAsync_OnUpdateOfNonDeletedEntity_LeavesDeletionColumnsEmpty()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.UserId.Returns("editor");

        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var product = NewProduct();
        context.Products.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.Update("Keyboard v2", "Updated description", Money.Create(59.99m, "USD").Value);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.DeletedOnUtc.ShouldBeNull();
        product.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAlreadyDeletedEntityIsSavedAgain_KeepsOriginalDeletionStamp()
    {
        var deletedAt = new DateTimeOffset(2026, 3, 1, 9, 15, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(deletedAt);
        _currentUser.UserId.Returns("deleter");

        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var product = NewProduct();
        context.Products.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        product.Delete();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dateTimeProvider.UtcNow.Returns(deletedAt.AddDays(1));
        _currentUser.UserId.Returns("someone-else");
        context.Products.Update(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        product.DeletedOnUtc.ShouldBe(deletedAt);
        product.DeletedBy.ShouldBe("deleter");
    }
}
