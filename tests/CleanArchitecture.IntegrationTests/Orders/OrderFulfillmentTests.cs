using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.Orders;
using CleanArchitecture.Application.Products;
using CleanArchitecture.IntegrationTests.Infrastructure;
using CleanArchitecture.Presentation.Endpoints.Products;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Orders;

[Collection(IntegrationTestCollection.Name)]
public sealed class OrderFulfillmentTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Order_moves_from_placed_to_paid_shipped_and_completed()
    {
        var (_, orderId) = await GivenAnOrderAsync(stock: 5, quantity: 1);
        using var customer = CreateCustomerClient();
        using var fulfillment = CreateFulfillmentClient();

        (await PostAsync(customer, orderId, "pay")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await StatusOfAsync(customer, orderId)).ShouldBe("Paid");

        (await PostAsync(fulfillment, orderId, "ship")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await StatusOfAsync(customer, orderId)).ShouldBe("Shipped");

        (await PostAsync(fulfillment, orderId, "complete")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await StatusOfAsync(customer, orderId)).ShouldBe("Completed");
    }

    [Fact]
    public async Task Shipping_an_unpaid_order_returns_conflict_problem()
    {
        var (_, orderId) = await GivenAnOrderAsync(stock: 5, quantity: 1);
        using var fulfillment = CreateFulfillmentClient();

        var response = await PostAsync(fulfillment, orderId, "ship");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull().Title.ShouldBe("Order.InvalidStatusTransition");
    }

    [Fact]
    public async Task Cancelling_a_shipped_order_returns_conflict_problem()
    {
        var (_, orderId) = await GivenAnOrderAsync(stock: 5, quantity: 1);
        using var customer = CreateCustomerClient();
        using var fulfillment = CreateFulfillmentClient();
        await PostAsync(customer, orderId, "pay");
        await PostAsync(fulfillment, orderId, "ship");

        var response = await PostAsync(customer, orderId, "cancel");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Paying_another_customers_order_returns_not_found()
    {
        var (_, orderId) = await GivenAnOrderAsync(stock: 5, quantity: 1);
        using var otherCustomer = CreateCustomerClient(OtherCustomerSubject);

        var response = await PostAsync(otherCustomer, orderId, "pay");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Placing_an_order_reserves_stock_and_cancelling_releases_it()
    {
        var (productId, orderId) = await GivenAnOrderAsync(stock: 5, quantity: 3);
        using var writer = CreateWriterClient();
        using var customer = CreateCustomerClient();

        (await StockOfAsync(writer, productId)).ShouldBe(2);

        (await PostAsync(customer, orderId, "cancel")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await StockOfAsync(writer, productId)).ShouldBe(5);
    }

    [Fact]
    public async Task Ordering_more_than_the_stock_returns_conflict_and_reserves_nothing()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct() with { StockQuantity = 2 });
        using var customer = CreateCustomerClient();

        var response = await customer.PostAsJsonAsync("/api/orders", NewOrder((productId, 3)), CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull().Title.ShouldBe("Product.InsufficientStock");
        (await StockOfAsync(writer, productId)).ShouldBe(2);
        (await customer.GetFromJsonAsync<List<OrderResponse>>("/api/orders", CancellationToken)).ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public async Task Setting_stock_replaces_it_and_rejects_negative_values()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct());

        var ok = await writer.PutAsJsonAsync(
            $"/api/products/{productId}/stock", new SetProductStockRequest { StockQuantity = 7 }, CancellationToken);
        var negative = await writer.PutAsJsonAsync(
            $"/api/products/{productId}/stock", new SetProductStockRequest { StockQuantity = -1 }, CancellationToken);
        var unknown = await writer.PutAsJsonAsync(
            $"/api/products/{Guid.NewGuid()}/stock", new SetProductStockRequest { StockQuantity = 1 }, CancellationToken);

        ok.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        negative.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await StockOfAsync(writer, productId)).ShouldBe(7);
    }

    private async Task<(Guid ProductId, Guid OrderId)> GivenAnOrderAsync(int stock, int quantity)
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct() with { StockQuantity = stock });
        using var customer = CreateCustomerClient();
        var orderId = await PlaceOrderAsync(customer, NewOrder((productId, quantity)));

        return (productId, orderId);
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, Guid orderId, string action) =>
        client.PostAsync($"/api/orders/{orderId}/{action}", content: null, CancellationToken);

    private static async Task<string> StatusOfAsync(HttpClient client, Guid orderId) =>
        (await client.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}", CancellationToken)).ShouldNotBeNull().Status;

    private static async Task<int> StockOfAsync(HttpClient client, Guid productId) =>
        (await client.GetFromJsonAsync<ProductResponse>($"/api/products/{productId}", CancellationToken))
            .ShouldNotBeNull().StockQuantity;
}
