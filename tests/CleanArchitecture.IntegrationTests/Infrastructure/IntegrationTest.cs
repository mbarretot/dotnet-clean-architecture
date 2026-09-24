using System.Net.Http.Json;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Endpoints.Orders;
using CleanArchitecture.Presentation.Endpoints.Products;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Infrastructure;

/// <summary>Starts every test from an empty database and provides clients for the common caller identities.</summary>
public abstract class IntegrationTest(ApiFactory factory) : IAsyncLifetime
{
    protected const string WriterSubject = "writer-user";

    protected const string ReaderSubject = "reader-user";

    protected const string CustomerSubject = "customer-user";

    protected const string OtherCustomerSubject = "other-customer-user";

    protected ApiFactory Factory { get; } = factory;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>Authenticated caller granted <see cref="Scopes.ProductsWrite"/>.</summary>
    protected HttpClient CreateWriterClient() =>
        Factory.CreateClient(TestJwtTokens.Create(WriterSubject, Scopes.ProductsWrite));

    /// <summary>Authenticated caller without any scope: may read, must not write.</summary>
    protected HttpClient CreateReaderClient() => Factory.CreateClient(TestJwtTokens.Create(ReaderSubject));

    /// <summary>Authenticated caller granted <see cref="Scopes.OrdersWrite"/>, identified by <paramref name="subject"/>.</summary>
    protected HttpClient CreateCustomerClient(string subject = CustomerSubject) =>
        Factory.CreateClient(TestJwtTokens.Create(subject, Scopes.OrdersWrite));

    protected HttpClient CreateAnonymousClient() => Factory.CreateClient(accessToken: null);

    protected static CreateProductRequest NewProduct(string name = "Mechanical Keyboard", string sku = "KB-001") => new()
    {
        Name = name,
        Description = "Hot-swappable switches",
        Price = 129.99m,
        Currency = "USD",
        Sku = sku,
    };

    protected static async Task<Guid> CreateProductAsync(HttpClient client, CreateProductRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/products", request, CancellationToken);
        response.EnsureSuccessStatusCode();

        var id = await response.Content.ReadFromJsonAsync<Guid>(CancellationToken);
        id.ShouldNotBe(Guid.Empty);

        return id;
    }

    protected static PlaceOrderRequest NewOrder(params (Guid ProductId, int Quantity)[] lines) => new()
    {
        Lines = lines.Select(line => new PlaceOrderLineRequest { ProductId = line.ProductId, Quantity = line.Quantity }).ToList(),
    };

    protected static async Task<Guid> PlaceOrderAsync(HttpClient client, PlaceOrderRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/orders", request, CancellationToken);
        response.EnsureSuccessStatusCode();

        var id = await response.Content.ReadFromJsonAsync<Guid>(CancellationToken);
        id.ShouldNotBe(Guid.Empty);

        return id;
    }

    public async ValueTask InitializeAsync() => await Factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
