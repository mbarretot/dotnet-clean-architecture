using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.IntegrationTests.Infrastructure;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Orders;

/// <summary>Exercises the real JwtBearer handler and the real <c>orders:write</c> scope policy end to end.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class OrderAuthorizationTests(ApiFactory factory) : IntegrationTest(factory)
{
    public static TheoryData<string> AllOrderEndpoints =>
        ["GET /api/orders", "GET /api/orders/{id}", "POST /api/orders", "POST /api/orders/{id}/cancel"];

    public static TheoryData<string> WriteOrderEndpoints => ["POST /api/orders", "POST /api/orders/{id}/cancel"];

    [Theory]
    [MemberData(nameof(AllOrderEndpoints))]
    public async Task Request_without_token_is_unauthorized(string endpoint)
    {
        using var client = CreateAnonymousClient();

        var response = await SendAsync(client, endpoint, Guid.NewGuid(), Guid.NewGuid());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(WriteOrderEndpoints))]
    public async Task Write_without_orders_write_scope_is_forbidden(string endpoint)
    {
        var (productId, orderId) = await GivenAnOrderAsync();
        using var reader = CreateReaderClient();

        var response = await SendAsync(reader, endpoint, orderId, productId);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(WriteOrderEndpoints))]
    public async Task Write_with_only_products_write_scope_is_forbidden(string endpoint)
    {
        var (productId, orderId) = await GivenAnOrderAsync();
        using var productWriter = CreateWriterClient();

        var response = await SendAsync(productWriter, endpoint, orderId, productId);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Read_without_orders_write_scope_is_allowed()
    {
        using var reader = CreateReaderClient();

        var response = await reader.GetAsync("/api/orders", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<(Guid ProductId, Guid OrderId)> GivenAnOrderAsync()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct());
        using var customer = CreateCustomerClient();
        var orderId = await PlaceOrderAsync(customer, NewOrder((productId, 1)));

        return (productId, orderId);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string endpoint, Guid orderId, Guid productId)
    {
        var (method, template) = (endpoint.Split(' ')[0], endpoint.Split(' ')[1]);
        var uri = template.Replace("{id}", orderId.ToString(), StringComparison.Ordinal);

        return (method, template) switch
        {
            ("GET", _) => client.GetAsync(uri, CancellationToken),
            ("POST", "/api/orders") => client.PostAsJsonAsync(uri, NewOrder((productId, 1)), CancellationToken),
            ("POST", _) => client.PostAsync(uri, content: null, CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Unsupported HTTP method."),
        };
    }
}
