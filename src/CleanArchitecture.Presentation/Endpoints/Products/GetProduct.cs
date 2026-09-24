using CleanArchitecture.Application.Products;
using CleanArchitecture.Application.Products.GetProductById;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Presentation.Endpoints.Products;

public sealed class GetProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/products/{id:guid}", HandleAsync)
            .WithName("GetProduct")
            .WithTags(ProductEndpointTags.Products)
            .RequireAuthorization()
            .WithSummary("Get a product by id")
            .WithDescription("Returns the product with the given identifier.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken).ConfigureAwait(false);

        return result.Match(product => TypedResults.Ok(product), error => error.ToProblem());
    }
}
