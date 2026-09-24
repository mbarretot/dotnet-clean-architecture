using CleanArchitecture.Application.Orders;
using CleanArchitecture.Application.Orders.GetOrderById;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Presentation.Endpoints.Orders;

/// <summary><c>GET /api/orders/{id}</c> — returns one of the caller's orders.</summary>
public sealed class GetOrder : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/orders/{id:guid}", HandleAsync)
            .WithName("GetOrder")
            .WithTags(OrderEndpointTags.Orders)
            .RequireAuthorization()
            .WithSummary("Get one of my orders by id")
            .WithDescription("Returns the caller's order with the given identifier. Another customer's order returns 404.")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken).ConfigureAwait(false);

        return result.Match(order => TypedResults.Ok(order), error => error.ToProblem());
    }
}
