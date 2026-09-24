using CleanArchitecture.Application.Products.DeleteProduct;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Presentation.Endpoints.Products;

/// <summary><c>DELETE /api/products/{id}</c> — soft-deletes a product; afterwards it reads as not found.</summary>
public sealed class DeleteProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/products/{id:guid}", HandleAsync)
            .WithName("DeleteProduct")
            .WithTags(ProductEndpointTags.Products)
            .RequireAuthorization(AuthorizationPolicies.ProductsWrite)
            .WithSummary("Delete a product")
            .WithDescription(
                "Soft-deletes the product with the given identifier. The record is kept for history, but the product is " +
                "no longer returned, and its SKU can be reused. Deleting an unknown or already deleted product returns 404.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProductCommand(id), cancellationToken).ConfigureAwait(false);

        return result.ToNoContentResult();
    }
}
