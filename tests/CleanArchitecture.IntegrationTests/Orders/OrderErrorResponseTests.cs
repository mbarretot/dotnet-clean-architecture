using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Orders;

[Collection(IntegrationTestCollection.Name)]
public sealed class OrderErrorResponseTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string ProblemJson = "application/problem+json";

    [Fact]
    public async Task Place_without_lines_returns_validation_problem()
    {
        using var customer = CreateCustomerClient();

        var response = await customer.PostAsJsonAsync("/api/orders", NewOrder(), CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemJson);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull().Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Errors.Keys.ShouldBe(["Lines"]);
    }

    [Fact]
    public async Task Place_with_non_positive_quantity_returns_validation_problem()
    {
        using var customer = CreateCustomerClient();

        var response = await customer.PostAsJsonAsync("/api/orders", NewOrder((Guid.NewGuid(), 0)), CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull().Errors.Keys.ShouldBe(["Lines[0].Quantity"]);
    }

    [Fact]
    public async Task Place_with_unknown_product_returns_not_found_problem()
    {
        using var customer = CreateCustomerClient();

        var response = await customer.PostAsJsonAsync("/api/orders", NewOrder((Guid.NewGuid(), 1)), CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Order.ProductNotFound");
    }

    [Fact]
    public async Task Place_with_deleted_product_returns_not_found_problem()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct());
        (await writer.DeleteAsync($"/api/products/{productId}", CancellationToken)).EnsureSuccessStatusCode();
        using var customer = CreateCustomerClient();

        var response = await customer.PostAsJsonAsync("/api/orders", NewOrder((productId, 1)), CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Order.ProductNotFound");
    }

    [Fact]
    public async Task Place_with_products_in_different_currencies_returns_bad_request_problem()
    {
        using var writer = CreateWriterClient();
        var usdId = await CreateProductAsync(writer, NewProduct("Dollar", "SKU-USD"));
        var eurId = await CreateProductAsync(writer, NewProduct("Euro", "SKU-EUR") with { Currency = "EUR" });
        using var customer = CreateCustomerClient();

        var response = await customer.PostAsJsonAsync("/api/orders", NewOrder((usdId, 1), (eurId, 1)), CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Order.CurrencyMismatch");
    }

    [Fact]
    public async Task Get_unknown_order_returns_not_found_problem()
    {
        using var customer = CreateCustomerClient();

        var response = await customer.GetAsync($"/api/orders/{Guid.NewGuid()}", CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Order.NotFound");
    }

    [Fact]
    public async Task Another_customers_order_reads_as_not_found_and_cannot_be_cancelled()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct());
        using var owner = CreateCustomerClient();
        var orderId = await PlaceOrderAsync(owner, NewOrder((productId, 1)));
        using var intruder = CreateCustomerClient(OtherCustomerSubject);

        var get = await intruder.GetAsync($"/api/orders/{orderId}", CancellationToken);
        var cancel = await intruder.PostAsync($"/api/orders/{orderId}/cancel", content: null, CancellationToken);
        var list = await intruder.GetFromJsonAsync<List<object>>("/api/orders", CancellationToken);

        await ShouldBeProblemAsync(get, HttpStatusCode.NotFound, "Order.NotFound");
        await ShouldBeProblemAsync(cancel, HttpStatusCode.NotFound, "Order.NotFound");
        list.ShouldNotBeNull().ShouldBeEmpty();
        (await owner.GetAsync($"/api/orders/{orderId}", CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cancelling_an_already_cancelled_order_returns_conflict_problem()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct());
        using var customer = CreateCustomerClient();
        var orderId = await PlaceOrderAsync(customer, NewOrder((productId, 1)));
        (await customer.PostAsync($"/api/orders/{orderId}/cancel", content: null, CancellationToken)).EnsureSuccessStatusCode();

        var response = await customer.PostAsync($"/api/orders/{orderId}/cancel", content: null, CancellationToken);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "Order.AlreadyCancelled");
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
