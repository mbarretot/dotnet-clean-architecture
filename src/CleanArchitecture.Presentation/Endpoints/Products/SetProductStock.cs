using CleanArchitecture.Application.Products.SetProductStock;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Presentation.Endpoints.Products;

public sealed record SetProductStockRequest
{
    public required int StockQuantity { get; init; }
}

public sealed class SetProductStock : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/products/{id:guid}/stock", HandleAsync)
            .WithName("SetProductStock")
            .WithTags(ProductEndpointTags.Products)
            .RequireAuthorization(AuthorizationPolicies.ProductsWrite)
            .WithSummary("Set a product's stock")
            .WithDescription("Replaces the number of units on hand. Placing an order reserves stock; cancelling releases it.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id, SetProductStockRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender
            .Send(new SetProductStockCommand(id, request.StockQuantity), cancellationToken)
            .ConfigureAwait(false);

        return result.ToNoContentResult();
    }
}
