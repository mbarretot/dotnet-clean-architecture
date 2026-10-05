using CleanArchitecture.Application.Orders.ShipOrder;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Presentation.Endpoints.Orders;

public sealed class ShipOrder : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/orders/{id:guid}/ship", HandleAsync)
            .WithName("ShipOrder")
            .WithTags(OrderEndpointTags.Orders)
            .RequireAuthorization(AuthorizationPolicies.OrdersFulfill)
            .WithSummary("Ship an order")
            .WithDescription("Fulfilment: marks a paid order as shipped. Any other status returns 409.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ShipOrderCommand(id), cancellationToken).ConfigureAwait(false);

        return result.ToNoContentResult();
    }
}
