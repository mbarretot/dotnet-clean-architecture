using CleanArchitecture.Application.Orders.PlaceOrder;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Presentation.Endpoints.Orders;

public sealed record PlaceOrderRequest
{
    public required IReadOnlyList<PlaceOrderLineRequest> Lines { get; init; }
}

public sealed record PlaceOrderLineRequest
{
    public required Guid ProductId { get; init; }

    public required int Quantity { get; init; }
}

/// <summary><c>POST /api/orders</c> — places an order for the caller.</summary>
public sealed class PlaceOrder : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/orders", HandleAsync)
            .WithName("PlaceOrder")
            .WithTags(OrderEndpointTags.Orders)
            .RequireAuthorization(AuthorizationPolicies.OrdersWrite)
            .WithSummary("Place an order")
            .WithDescription(
                "Places an order for the authenticated caller and returns the identifier of the new resource. Each " +
                "line snapshots the product's current name and price; lines for the same product are merged. " +
                "Unknown or deleted products return 404, and all products must share one currency.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        PlaceOrderRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(
            (request.Lines ?? []).Select(line => new PlaceOrderLine(line.ProductId, line.Quantity)).ToList());

        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.Match(
            id => TypedResults.Created($"/api/orders/{id}", id),
            error => error.ToProblem());
    }
}
