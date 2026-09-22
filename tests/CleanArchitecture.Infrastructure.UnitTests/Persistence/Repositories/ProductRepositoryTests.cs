using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.Infrastructure.Persistence.Configurations;
using CleanArchitecture.Infrastructure.Persistence.Repositories;
using CleanArchitecture.Infrastructure.UnitTests.Persistence.Interceptors;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Infrastructure.UnitTests.Persistence.Repositories;

/// <summary>Proves the value-object mappings (<see cref="Sku"/>, <see cref="Money"/>) round-trip through real SQL.</summary>
public sealed class ProductRepositoryTests : IDisposable
{
    private readonly SqliteApplicationDbContextFixture _fixture = new();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ExistsBySkuAsync_WhenSkuAlreadyPersisted_ReturnsTrueRegardlessOfInputCasing()
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = new ProductRepository(context);
        var product = Product.Create(
            "Keyboard", "Mechanical keyboard", Money.Create(49.99m, "USD").Value, Sku.Create("SKU-REPO-1").Value).Value;
        repository.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var existsExactCase = await repository.ExistsBySkuAsync("SKU-REPO-1", TestContext.Current.CancellationToken);
        var existsLowerCase = await repository.ExistsBySkuAsync("sku-repo-1", TestContext.Current.CancellationToken);
        var existsOther = await repository.ExistsBySkuAsync("SKU-REPO-2", TestContext.Current.CancellationToken);

        existsExactCase.ShouldBeTrue();
        existsLowerCase.ShouldBeTrue();
        existsOther.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPersistedProductWithPriceAndSkuIntact()
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = new ProductRepository(context);
        var product = Product.Create(
            "Mouse", "Wireless mouse", Money.Create(29.99m, "USD").Value, Sku.Create("SKU-REPO-3").Value).Value;
        repository.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var loaded = await repository.GetByIdAsync(product.Id, TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.Sku.Value.ShouldBe("SKU-REPO-3");
        loaded.Price.Amount.ShouldBe(29.99m);
        loaded.Price.Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task GetAllAsync_PagesResultsOrderedByName()
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = new ProductRepository(context);
        repository.Add(Product.Create("Zebra Mat", "d", Money.Create(1m, "USD").Value, Sku.Create("SKU-A").Value).Value);
        repository.Add(Product.Create("Apple Stand", "d", Money.Create(1m, "USD").Value, Sku.Create("SKU-B").Value).Value);
        repository.Add(Product.Create("Mango Case", "d", Money.Create(1m, "USD").Value, Sku.Create("SKU-C").Value).Value);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var firstPage = await repository.GetAllAsync(pageNumber: 1, pageSize: 2, TestContext.Current.CancellationToken);
        var secondPage = await repository.GetAllAsync(pageNumber: 2, pageSize: 2, TestContext.Current.CancellationToken);

        firstPage.Select(product => product.Name).ShouldBe(["Apple Stand", "Mango Case"]);
        secondPage.Select(product => product.Name).ShouldBe(["Zebra Mat"]);
    }
}
