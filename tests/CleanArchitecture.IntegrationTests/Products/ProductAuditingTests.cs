using System.Net.Http.Json;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.IntegrationTests.Infrastructure;
using CleanArchitecture.Presentation.Endpoints.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Products;

/// <summary>The API does not expose audit columns, so they are read back straight from the database.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ProductAuditingTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Created_product_records_the_callers_subject()
    {
        using var client = CreateWriterClient();

        var id = await CreateProductAsync(client, NewProduct());

        var product = await FindProductAsync(id);
        product.CreatedBy.ShouldBe(WriterSubject);
        product.CreatedAt.ShouldNotBe(default);
        product.ModifiedBy.ShouldBeNull();
    }

    [Fact]
    public async Task Updated_product_records_the_callers_subject()
    {
        using var client = CreateWriterClient();
        var id = await CreateProductAsync(client, NewProduct());
        var update = new UpdateProductRequest { Name = "Renamed", Description = "", Price = 10m, Currency = "USD" };

        (await client.PutAsJsonAsync($"/api/products/{id}", update, CancellationToken)).EnsureSuccessStatusCode();

        var product = await FindProductAsync(id);
        product.ModifiedBy.ShouldBe(WriterSubject);
        product.ModifiedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Deleted_product_is_kept_and_records_the_callers_subject()
    {
        using var client = CreateWriterClient();
        var id = await CreateProductAsync(client, NewProduct());

        (await client.DeleteAsync($"/api/products/{id}", CancellationToken)).EnsureSuccessStatusCode();

        var product = await FindProductAsync(id);
        product.IsDeleted.ShouldBeTrue();
        product.DeletedBy.ShouldBe(WriterSubject);
        product.DeletedOnUtc.ShouldNotBeNull();
    }

    /// <summary>Bypasses the soft-delete filter so deleted rows can be inspected too.</summary>
    private async Task<Product> FindProductAsync(Guid id)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.Products
            .AsNoTracking()
            .IgnoreQueryFilters([ApplicationDbContext.SoftDeleteFilter])
            .SingleAsync(product => product.Id == id, CancellationToken);
    }
}
