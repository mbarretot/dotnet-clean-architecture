using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.Orders;
using CleanArchitecture.IntegrationTests.Infrastructure;
using CleanArchitecture.Presentation.Endpoints.Products;
using Shouldly;

namespace CleanArchitecture.IntegrationTests.Orders;

[Collection(IntegrationTestCollection.Name)]
public sealed class OrderLifecycleTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Placed_order_returns_created_with_product_snapshots_and_total()
    {
        using var writer = CreateWriterClient();
        var keyboardId = await CreateProductAsync(writer, NewProduct("Keyboard", "SKU-KB"));
        var mouseId = await CreateProductAsync(writer, NewProduct("Mouse", "SKU-MS") with { Price = 20m });
        using var customer = CreateCustomerClient();

        var response = await customer.PostAsJsonAsync("/api/orders", NewOrder((keyboardId, 2), (mouseId, 1)), CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>(CancellationToken);
        response.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe($"/api/orders/{id}");

        var order = await customer.GetFromJsonAsync<OrderResponse>($"/api/orders/{id}", CancellationToken);
        order.ShouldNotBeNull();
        order.Id.ShouldBe(id);
        order.Status.ShouldBe("Placed");
        order.Currency.ShouldBe("USD");
        order.Total.ShouldBe(279.98m);
        order.PlacedAt.ShouldNotBe(default);
        order.Lines.Count.ShouldBe(2);
        order.Lines.ShouldContain(new OrderLineResponse(keyboardId, "Keyboard", 129.99m, 2, 259.98m));
        order.Lines.ShouldContain(new OrderLineResponse(mouseId, "Mouse", 20m, 1, 20m));
    }

    [Fact]
    public async Task Ordering_the_same_product_twice_merges_the_lines()
    {
        using var writer = CreateWriterClient();
        var keyboardId = await CreateProductAsync(writer, NewProduct());
        using var customer = CreateCustomerClient();

        var id = await PlaceOrderAsync(customer, NewOrder((keyboardId, 1), (keyboardId, 2)));

        var order = await customer.GetFromJsonAsync<OrderResponse>($"/api/orders/{id}", CancellationToken);
        order.ShouldNotBeNull().Lines.ShouldHaveSingleItem().Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task Price_snapshot_is_kept_after_the_product_price_changes()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct("Keyboard", "SKU-KB"));
        using var customer = CreateCustomerClient();
        var orderId = await PlaceOrderAsync(customer, NewOrder((productId, 1)));

        var update = new UpdateProductRequest { Name = "Keyboard v2", Description = "", Price = 10m, Currency = "USD" };
        (await writer.PutAsJsonAsync($"/api/products/{productId}", update, CancellationToken)).EnsureSuccessStatusCode();

        var order = await customer.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}", CancellationToken);
        order.ShouldNotBeNull().Total.ShouldBe(129.99m);
        order.Lines.ShouldHaveSingleItem().ShouldBe(new OrderLineResponse(productId, "Keyboard", 129.99m, 1, 129.99m));
    }

    [Fact]
    public async Task Order_stays_readable_after_its_product_is_deleted()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct());
        using var customer = CreateCustomerClient();
        var orderId = await PlaceOrderAsync(customer, NewOrder((productId, 1)));

        (await writer.DeleteAsync($"/api/products/{productId}", CancellationToken)).EnsureSuccessStatusCode();

        var order = await customer.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}", CancellationToken);
        order.ShouldNotBeNull().Lines.ShouldHaveSingleItem().ProductName.ShouldBe("Mechanical Keyboard");
    }

    [Fact]
    public async Task Cancel_returns_no_content_and_a_second_cancel_returns_conflict()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct());
        using var customer = CreateCustomerClient();
        var orderId = await PlaceOrderAsync(customer, NewOrder((productId, 1)));

        var first = await customer.PostAsync($"/api/orders/{orderId}/cancel", content: null, CancellationToken);
        var second = await customer.PostAsync($"/api/orders/{orderId}/cancel", content: null, CancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var order = await customer.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}", CancellationToken);
        order.ShouldNotBeNull().Status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task Listing_returns_only_the_callers_orders_newest_first_and_paged()
    {
        using var writer = CreateWriterClient();
        var productId = await CreateProductAsync(writer, NewProduct());
        using var customer = CreateCustomerClient();
        using var otherCustomer = CreateCustomerClient(OtherCustomerSubject);
        var oldest = await PlaceOrderAsync(customer, NewOrder((productId, 1)));
        var middle = await PlaceOrderAsync(customer, NewOrder((productId, 2)));
        var newest = await PlaceOrderAsync(customer, NewOrder((productId, 3)));
        await PlaceOrderAsync(otherCustomer, NewOrder((productId, 1)));

        var firstPage = await customer.GetFromJsonAsync<List<OrderResponse>>("/api/orders?pageNumber=1&pageSize=2", CancellationToken);
        var secondPage = await customer.GetFromJsonAsync<List<OrderResponse>>("/api/orders?pageNumber=2&pageSize=2", CancellationToken);

        firstPage.ShouldNotBeNull().Select(order => order.Id).ShouldBe([newest, middle]);
        secondPage.ShouldNotBeNull().Select(order => order.Id).ShouldBe([oldest]);
    }
}
