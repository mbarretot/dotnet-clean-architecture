using CleanArchitecture.Application.Orders;
using CleanArchitecture.Application.Orders.GetMyOrders;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Presentation.Endpoints.Orders;

/// <summary><c>GET /api/orders</c> — returns a page of the caller's orders.</summary>
public sealed class GetMyOrders : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/orders", HandleAsync)
            .WithName("GetMyOrders")
            .WithTags(OrderEndpointTags.Orders)
            .RequireAuthorization()
            .WithSummary("List my orders")
            .WithDescription("Returns a page of the caller's orders, newest first.")
            .Produces<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        ISender sender, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 20)
    {
        var result = await sender
            .Send(new GetMyOrdersQuery(pageNumber, pageSize), cancellationToken)
            .ConfigureAwait(false);

        return result.Match(orders => TypedResults.Ok(orders), error => error.ToProblem());
    }
}
