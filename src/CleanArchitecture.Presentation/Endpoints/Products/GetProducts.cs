using CleanArchitecture.Application.Products;
using CleanArchitecture.Application.Products.GetProducts;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Presentation.Endpoints.Products;

public sealed class GetProducts : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/products", HandleAsync)
            .WithName("GetProducts")
            .WithTags(ProductEndpointTags.Products)
            .RequireAuthorization()
            .WithSummary("List products")
            .WithDescription("Returns a page of products ordered by name.")
            .Produces<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        ISender sender, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 20)
    {
        var result = await sender
            .Send(new GetProductsQuery(pageNumber, pageSize), cancellationToken)
            .ConfigureAwait(false);

        return result.Match(products => TypedResults.Ok(products), error => error.ToProblem());
    }
}
