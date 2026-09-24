using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.IntegrationTests.Infrastructure;
using CleanArchitecture.Presentation.Endpoints.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Products;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProductErrorResponseTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string ProblemJson = "application/problem+json";

    [Fact]
    public async Task Create_with_invalid_input_returns_validation_problem()
    {
        using var client = CreateWriterClient();
        var request = NewProduct() with { Name = "", Currency = "DOLLARS" };

        var response = await client.PostAsJsonAsync("/api/products", request, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemJson);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull().Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Errors.Keys.ShouldBe(["Name", "Currency"], ignoreOrder: true);
    }

    [Fact]
    public async Task Update_with_invalid_input_returns_validation_problem()
    {
        using var client = CreateWriterClient();
        var id = await CreateProductAsync(client, NewProduct());
        var request = new UpdateProductRequest { Name = "Keyboard", Description = "", Price = -1m, Currency = "USD" };

        var response = await client.PutAsJsonAsync($"/api/products/{id}", request, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull().Errors.Keys.ShouldBe(["Price"]);
    }

    [Fact]
    public async Task Get_unknown_product_returns_not_found_problem()
    {
        using var client = CreateReaderClient();
        var id = Guid.NewGuid();

        var response = await client.GetAsync($"/api/products/{id}", CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Product.NotFound");
    }

    [Fact]
    public async Task Update_unknown_product_returns_not_found_problem()
    {
        using var client = CreateWriterClient();
        var request = new UpdateProductRequest { Name = "Keyboard", Description = "", Price = 1m, Currency = "USD" };

        var response = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", request, CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Product.NotFound");
    }

    [Fact]
    public async Task Delete_unknown_product_returns_not_found_problem()
    {
        using var client = CreateWriterClient();

        var response = await client.DeleteAsync($"/api/products/{Guid.NewGuid()}", CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Product.NotFound");
    }

    [Fact]
    public async Task Create_with_duplicate_sku_returns_conflict_problem()
    {
        using var client = CreateWriterClient();
        await CreateProductAsync(client, NewProduct("First", "DUP-001"));

        var response = await client.PostAsJsonAsync("/api/products", NewProduct("Second", "DUP-001"), CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "Product.SkuAlreadyExists");
    }

    [Fact]
    public async Task Deleting_an_already_deleted_product_returns_not_found_problem()
    {
        using var client = CreateWriterClient();
        var id = await CreateProductAsync(client, NewProduct());
        (await client.DeleteAsync($"/api/products/{id}", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await client.DeleteAsync($"/api/products/{id}", CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Product.NotFound");
    }

    [Fact]
    public async Task Get_deleted_product_returns_not_found_problem()
    {
        using var client = CreateWriterClient();
        var id = await CreateProductAsync(client, NewProduct());
        (await client.DeleteAsync($"/api/products/{id}", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await client.GetAsync($"/api/products/{id}", CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Product.NotFound");
    }

    [Fact]
    public async Task Update_deleted_product_returns_not_found_problem()
    {
        using var client = CreateWriterClient();
        var id = await CreateProductAsync(client, NewProduct());
        (await client.DeleteAsync($"/api/products/{id}", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var request = new UpdateProductRequest { Name = "Keyboard", Description = "", Price = 1m, Currency = "USD" };

        var response = await client.PutAsJsonAsync($"/api/products/{id}", request, CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Product.NotFound");
    }

    private static async Task ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode statusCode, string title)
    {
        response.StatusCode.ShouldBe(statusCode);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemJson);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe((int)statusCode);
        problem.Title.ShouldBe(title);
    }
}
