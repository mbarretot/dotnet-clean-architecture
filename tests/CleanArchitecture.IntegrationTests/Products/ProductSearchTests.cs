using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.Products;
using CleanArchitecture.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Products;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProductSearchTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Theory]
    [InlineData("?search=KEYBOARD", new[] { "Mechanical Keyboard" })]
    [InlineData("?search=wireless&sort=-price", new[] { "Mechanical Keyboard", "Mouse" })]
    [InlineData("?minPrice=50&maxPrice=300", new[] { "Mechanical Keyboard", "Monitor" })]
    [InlineData("?sort=price", new[] { "Mouse", "Mechanical Keyboard", "Monitor" })]
    [InlineData("?sort=-name&pageSize=2", new[] { "Mouse", "Monitor" })]
    [InlineData("?sort=-name&pageSize=2&pageNumber=2", new[] { "Mechanical Keyboard" })]
    public async Task Listing_filters_sorts_and_pages(string queryString, string[] expected)
    {
        using var client = await GivenCatalogAsync();

        var products = await client.GetFromJsonAsync<List<ProductResponse>>($"/api/products{queryString}", CancellationToken);

        products.ShouldNotBeNull().Select(product => product.Name).ShouldBe(expected);
    }

    [Theory]
    [InlineData("?pageSize=101", "PageSize")]
    [InlineData("?pageNumber=0", "PageNumber")]
    [InlineData("?sort=stock", "Sort")]
    [InlineData("?minPrice=10&maxPrice=5", "MaxPrice")]
    public async Task Invalid_listing_parameters_return_validation_problem(string queryString, string field)
    {
        using var client = CreateReaderClient();

        var response = await client.GetAsync($"/api/products{queryString}", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull().Errors.Keys.ShouldContain(field);
    }

    [Fact]
    public async Task Cached_listing_reflects_a_product_created_afterwards()
    {
        using var client = await GivenCatalogAsync();
        (await client.GetFromJsonAsync<List<ProductResponse>>("/api/products", CancellationToken)).ShouldNotBeNull().Count.ShouldBe(3);

        await CreateProductAsync(client, NewProduct("Headset", "SKU-H1"));

        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products", CancellationToken);
        products.ShouldNotBeNull().Select(product => product.Name).ShouldContain("Headset");
    }

    private async Task<HttpClient> GivenCatalogAsync()
    {
        var client = CreateWriterClient();
        await CreateProductAsync(client, NewProduct("Mouse", "SKU-M1") with { Description = "Wireless optical mouse", Price = 20m });
        await CreateProductAsync(client, NewProduct("Mechanical Keyboard", "SKU-K1") with { Description = "Wireless, hot-swappable", Price = 80m });
        await CreateProductAsync(client, NewProduct("Monitor", "SKU-D1") with { Description = "27 inch display", Price = 300m });

        return client;
    }
}
