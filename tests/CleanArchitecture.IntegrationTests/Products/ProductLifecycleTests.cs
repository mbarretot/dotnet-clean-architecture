using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.Products;
using CleanArchitecture.IntegrationTests.Infrastructure;
using CleanArchitecture.Presentation.Endpoints.Products;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Products;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProductLifecycleTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Product_can_be_created_read_listed_updated_and_deleted()
    {
        using var client = CreateWriterClient();

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/products", NewProduct(), CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>(CancellationToken);
        createResponse.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe($"/api/products/{id}");

        // Get by id
        var created = await client.GetFromJsonAsync<ProductResponse>($"/api/products/{id}", CancellationToken);
        created.ShouldBe(new ProductResponse(id, "Mechanical Keyboard", "Hot-swappable switches", 129.99m, "USD", "KB-001", IsActive: true));

        // List
        var listed = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products", CancellationToken);
        listed.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBe(created);

        // Update
        var update = new UpdateProductRequest { Name = "Wireless Keyboard", Description = "Bluetooth", Price = 99.50m, Currency = "EUR" };
        var updateResponse = await client.PutAsJsonAsync($"/api/products/{id}", update, CancellationToken);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updated = await client.GetFromJsonAsync<ProductResponse>($"/api/products/{id}", CancellationToken);
        updated.ShouldBe(new ProductResponse(id, "Wireless Keyboard", "Bluetooth", 99.50m, "EUR", "KB-001", IsActive: true));

        // Delete is a soft delete (see DeleteProductCommandHandler): the product stays readable, but inactive.
        var deleteResponse = await client.DeleteAsync($"/api/products/{id}", CancellationToken);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var deleted = await client.GetFromJsonAsync<ProductResponse>($"/api/products/{id}", CancellationToken);
        deleted.ShouldNotBeNull().IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Listing_returns_the_requested_page_ordered_by_name()
    {
        using var client = CreateWriterClient();
        await CreateProductAsync(client, NewProduct("Charlie", "SKU-C"));
        await CreateProductAsync(client, NewProduct("Alpha", "SKU-A"));
        await CreateProductAsync(client, NewProduct("Bravo", "SKU-B"));

        var firstPage = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products?pageNumber=1&pageSize=2", CancellationToken);
        var secondPage = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products?pageNumber=2&pageSize=2", CancellationToken);

        firstPage.ShouldNotBeNull().Select(product => product.Name).ShouldBe(["Alpha", "Bravo"]);
        secondPage.ShouldNotBeNull().Select(product => product.Name).ShouldBe(["Charlie"]);
    }
}
