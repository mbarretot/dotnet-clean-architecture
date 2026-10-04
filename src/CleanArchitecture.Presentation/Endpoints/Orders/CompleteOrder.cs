using CleanArchitecture.Application.Orders.CompleteOrder;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Presentation.Endpoints.Orders;

public sealed class CompleteOrder : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/orders/{id:guid}/complete", HandleAsync)
            .WithName("CompleteOrder")
            .WithTags(OrderEndpointTags.Orders)
            .RequireAuthorization(AuthorizationPolicies.OrdersFulfill)
            .WithSummary("Complete an order")
            .WithDescription("Fulfilment: marks a shipped order as completed. Any other status returns 409.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteOrderCommand(id), cancellationToken).ConfigureAwait(false);

        return result.ToNoContentResult();
    }
}
