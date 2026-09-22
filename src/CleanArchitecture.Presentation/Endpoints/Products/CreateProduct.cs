using CleanArchitecture.Application.Products.CreateProduct;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Presentation.Endpoints.Products;

public sealed record CreateProductRequest
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required decimal Price { get; init; }

    public required string Currency { get; init; }

    public required string Sku { get; init; }
}

/// <summary><c>POST /api/products</c> — creates a new product.</summary>
public sealed class CreateProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/products", HandleAsync)
            .WithName("CreateProduct")
            .WithTags(ProductEndpointTags.Products)
            .WithSummary("Create a product")
            .WithDescription("Creates a new product in the catalog and returns the identifier of the new resource.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        CreateProductRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(request.Name, request.Description, request.Price, request.Currency, request.Sku);

        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.Match(
            id => TypedResults.Created($"/api/products/{id}", id),
            error => error.ToProblem());
    }
}
