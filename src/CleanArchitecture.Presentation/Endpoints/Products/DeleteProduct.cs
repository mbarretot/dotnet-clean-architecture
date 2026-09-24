using CleanArchitecture.Application.Products.DeleteProduct;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Presentation.Endpoints.Products;

/// <summary><c>DELETE /api/products/{id}</c> — deactivates (soft-deletes) a product.</summary>
public sealed class DeleteProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/products/{id:guid}", HandleAsync)
            .WithName("DeleteProduct")
            .WithTags(ProductEndpointTags.Products)
            .RequireAuthorization(AuthorizationPolicies.ProductsWrite)
            .WithSummary("Delete a product")
            .WithDescription("Deactivates the product with the given identifier, preserving its history.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProductCommand(id), cancellationToken).ConfigureAwait(false);

        return result.ToNoContentResult();
    }
}
