using CleanArchitecture.Application.Orders.PayOrder;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Presentation.Endpoints.Orders;

public sealed class PayOrder : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/orders/{id:guid}/pay", HandleAsync)
            .WithName("PayOrder")
            .WithTags(OrderEndpointTags.Orders)
            .RequireAuthorization(AuthorizationPolicies.OrdersWrite)
            .WithSummary("Pay one of my orders")
            .WithDescription("Marks the caller's placed order as paid. Any other status returns 409; another customer's order returns 404.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PayOrderCommand(id), cancellationToken).ConfigureAwait(false);

        return result.ToNoContentResult();
    }
}
