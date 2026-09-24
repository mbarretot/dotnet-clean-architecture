using System.Net;
using System.Net.Http.Json;
using System.Text;
using CleanArchitecture.IntegrationTests.Infrastructure;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Endpoints.Products;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Products;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProductAuthorizationTests(ApiFactory factory) : IntegrationTest(factory)
{
    private static readonly UpdateProductRequest Update =
        new() { Name = "Keyboard", Description = "", Price = 1m, Currency = "USD" };

    public static TheoryData<string> AllProductEndpoints =>
        ["GET /api/products", "GET /api/products/{id}", "POST /api/products", "PUT /api/products/{id}", "DELETE /api/products/{id}"];

    public static TheoryData<string> WriteProductEndpoints =>
        ["POST /api/products", "PUT /api/products/{id}", "DELETE /api/products/{id}"];

    [Theory]
    [MemberData(nameof(AllProductEndpoints))]
    public async Task Request_without_token_is_unauthorized(string endpoint)
    {
        using var client = CreateAnonymousClient();

        var response = await SendAsync(client, endpoint, Guid.NewGuid());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_with_token_signed_by_an_untrusted_key_is_unauthorized()
    {
        var untrustedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("an-attacker-controlled-key-0123456789abcdef"));
        using var client = Factory.CreateClient(TestJwtTokens.CreateSignedWith(untrustedKey, "attacker", Scopes.ProductsWrite));

        var response = await client.GetAsync("/api/products", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(WriteProductEndpoints))]
    public async Task Write_without_products_write_scope_is_forbidden(string endpoint)
    {
        using var writer = CreateWriterClient();
        var id = await CreateProductAsync(writer, NewProduct());
        using var reader = CreateReaderClient();

        var response = await SendAsync(reader, endpoint, id);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Read_without_products_write_scope_is_allowed()
    {
        using var writer = CreateWriterClient();
        var id = await CreateProductAsync(writer, NewProduct());
        using var reader = CreateReaderClient();

        var list = await reader.GetAsync("/api/products", CancellationToken);
        var single = await reader.GetAsync($"/api/products/{id}", CancellationToken);

        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        single.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string endpoint, Guid id)
    {
        var (method, template) = (endpoint.Split(' ')[0], endpoint.Split(' ')[1]);
        var uri = template.Replace("{id}", id.ToString(), StringComparison.Ordinal);

        return method switch
        {
            "GET" => client.GetAsync(uri, CancellationToken),
            "POST" => client.PostAsJsonAsync(uri, NewProduct("Other", "SKU-OTHER"), CancellationToken),
            "PUT" => client.PutAsJsonAsync(uri, Update, CancellationToken),
            "DELETE" => client.DeleteAsync(uri, CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Unsupported HTTP method."),
        };
    }
}
