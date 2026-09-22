using CleanArchitecture.Application.Products.UpdateProduct;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;

namespace CleanArchitecture.Presentation.Endpoints.Products;

public sealed record UpdateProductRequest
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required decimal Price { get; init; }

    public required string Currency { get; init; }
}

/// <summary><c>PUT /api/products/{id}</c> — updates an existing product.</summary>
public sealed class UpdateProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/products/{id:guid}", HandleAsync)
            .WithName("UpdateProduct")
            .WithTags(ProductEndpointTags.Products)
            .WithSummary("Update a product")
            .WithDescription("Updates an existing product's name, description and price.")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id, UpdateProductRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.Currency);

        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.ToOkResult();
    }
}
