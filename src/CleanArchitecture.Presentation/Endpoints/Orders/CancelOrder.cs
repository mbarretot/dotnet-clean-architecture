using CleanArchitecture.Application.Orders.CancelOrder;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Presentation.Endpoints.Orders;

public sealed class CancelOrder : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/orders/{id:guid}/cancel", HandleAsync)
            .WithName("CancelOrder")
            .WithTags(OrderEndpointTags.Orders)
            .RequireAuthorization(AuthorizationPolicies.OrdersWrite)
            .WithSummary("Cancel one of my orders")
            .WithDescription(
                "Cancels the caller's placed or paid order and releases its reserved stock. Cancelling an order that " +
                "is already cancelled, shipped or completed returns 409; another customer's order returns 404.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelOrderCommand(id), cancellationToken).ConfigureAwait(false);

        return result.ToNoContentResult();
    }
}
