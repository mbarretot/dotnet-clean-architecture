using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Configurations;
using CleanArchitecture.Infrastructure.Persistence.Repositories;
using CleanArchitecture.Infrastructure.UnitTests.Persistence.Interceptors;
using CleanArchitecture.SharedKernel.Abstractions;
using CleanArchitecture.SharedKernel.Messaging;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace CleanArchitecture.Infrastructure.UnitTests.Persistence.Repositories;

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
    public async Task SearchAsync_WithDefaultCriteria_PagesResultsOrderedByName()
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = new ProductRepository(context);
        repository.Add(Product.Create("Zebra Mat", "d", Money.Create(1m, "USD").Value, Sku.Create("SKU-A").Value).Value);
        repository.Add(Product.Create("Apple Stand", "d", Money.Create(1m, "USD").Value, Sku.Create("SKU-B").Value).Value);
        repository.Add(Product.Create("Mango Case", "d", Money.Create(1m, "USD").Value, Sku.Create("SKU-C").Value).Value);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var firstPage = await repository.SearchAsync(
            new ProductSearchCriteria(PageNumber: 1, PageSize: 2), TestContext.Current.CancellationToken);
        var secondPage = await repository.SearchAsync(
            new ProductSearchCriteria(PageNumber: 2, PageSize: 2), TestContext.Current.CancellationToken);

        firstPage.Select(product => product.Name).ShouldBe(["Apple Stand", "Mango Case"]);
        secondPage.Select(product => product.Name).ShouldBe(["Zebra Mat"]);
    }

    [Theory]
    [InlineData("keyboard", new[] { "Mechanical Keyboard" })]
    [InlineData("WIRELESS", new[] { "Mouse", "Mechanical Keyboard" })]
    [InlineData("cable", new string[0])]
    public async Task SearchAsync_WithSearchTerm_MatchesNameOrDescriptionIgnoringCase(string search, string[] expected)
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = await SeedCatalogAsync(context);

        var products = await repository.SearchAsync(
            new ProductSearchCriteria(Search: search, SortOrder: ProductSortOrder.PriceAscending),
            TestContext.Current.CancellationToken);

        products.Select(product => product.Name).ShouldBe(expected);
    }

    [Fact]
    public async Task SearchAsync_WithPriceRange_IncludesBothBounds()
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = await SeedCatalogAsync(context);

        var products = await repository.SearchAsync(
            new ProductSearchCriteria(MinPrice: 20m, MaxPrice: 80m), TestContext.Current.CancellationToken);

        products.Select(product => product.Name).ShouldBe(["Mechanical Keyboard", "Mouse"]);
    }

    [Theory]
    [InlineData(ProductSortOrder.NameAscending, new[] { "Mechanical Keyboard", "Monitor", "Mouse" })]
    [InlineData(ProductSortOrder.NameDescending, new[] { "Mouse", "Monitor", "Mechanical Keyboard" })]
    [InlineData(ProductSortOrder.PriceAscending, new[] { "Mouse", "Mechanical Keyboard", "Monitor" })]
    [InlineData(ProductSortOrder.PriceDescending, new[] { "Monitor", "Mechanical Keyboard", "Mouse" })]
    public async Task SearchAsync_SortsByTheRequestedOrder(ProductSortOrder sortOrder, string[] expected)
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = await SeedCatalogAsync(context);

        var products = await repository.SearchAsync(
            new ProductSearchCriteria(SortOrder: sortOrder), TestContext.Current.CancellationToken);

        products.Select(product => product.Name).ShouldBe(expected);
    }

    [Fact]
    public async Task StockQuantity_RoundTrips()
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = new ProductRepository(context);
        var product = Product.Create(
            "Stocked", "d", Money.Create(1m, "USD").Value, Sku.Create("SKU-STOCK").Value, stockQuantity: 4).Value;
        repository.Add(product);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var loaded = await repository.GetByIdAsync(product.Id, TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull().StockQuantity.ShouldBe(4);
    }

    [Fact]
    public async Task SoftDeletedProduct_IsHiddenFromQueriesButVisibleWhenSoftDeleteFilterIsIgnored()
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = new ProductRepository(context);
        var kept = NewProduct("Kept", "SKU-KEEP");
        var deleted = NewProduct("Deleted", "SKU-DEL");
        repository.Add(kept);
        repository.Add(deleted);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        deleted.Delete();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var byId = await repository.GetByIdAsync(deleted.Id, TestContext.Current.CancellationToken);
        var all = await repository.SearchAsync(new ProductSearchCriteria(), TestContext.Current.CancellationToken);
        var skuExists = await repository.ExistsBySkuAsync("SKU-DEL", TestContext.Current.CancellationToken);
        var unfiltered = await context.Products
            .IgnoreQueryFilters([ApplicationDbContext.SoftDeleteFilter])
            .SingleOrDefaultAsync(product => product.Id == deleted.Id, TestContext.Current.CancellationToken);

        byId.ShouldBeNull();
        all.Select(product => product.Id).ShouldBe([kept.Id]);
        skuExists.ShouldBeFalse();
        unfiltered.ShouldNotBeNull().IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Add_WithSkuOfSoftDeletedProduct_Succeeds()
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = new ProductRepository(context);
        var original = NewProduct("Original", "SKU-REUSE");
        repository.Add(original);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        original.Delete();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var replacement = NewProduct("Replacement", "SKU-REUSE");
        repository.Add(replacement);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var loaded = await repository.GetByIdAsync(replacement.Id, TestContext.Current.CancellationToken);
        loaded.ShouldNotBeNull().Sku.Value.ShouldBe("SKU-REUSE");
    }

    [Fact]
    public async Task Add_WithSkuOfActiveProduct_IsRejectedByUniqueIndex()
    {
        using var context = _fixture.CreateContext(_dateTimeProvider, _currentUser, _publisher);
        var repository = new ProductRepository(context);
        repository.Add(NewProduct("First", "SKU-DUP"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        repository.Add(NewProduct("Second", "SKU-DUP"));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    private static Product NewProduct(string name, string sku) =>
        Product.Create(name, "d", Money.Create(1m, "USD").Value, Sku.Create(sku).Value).Value;

    private static async Task<ProductRepository> SeedCatalogAsync(ApplicationDbContext context)
    {
        var repository = new ProductRepository(context);
        repository.Add(Product.Create("Mouse", "Wireless optical mouse", Money.Create(20m, "USD").Value, Sku.Create("SKU-M1").Value).Value);
        repository.Add(Product.Create("Mechanical Keyboard", "Wireless, hot-swappable", Money.Create(80m, "USD").Value, Sku.Create("SKU-K1").Value).Value);
        repository.Add(Product.Create("Monitor", "27 inch display", Money.Create(300m, "USD").Value, Sku.Create("SKU-D1").Value).Value);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        return repository;
    }
}
