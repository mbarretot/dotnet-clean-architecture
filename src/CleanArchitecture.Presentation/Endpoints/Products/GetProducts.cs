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
            .WithDescription(
                "Returns a page of products. `search` matches the name or description (case-insensitive), " +
                "`minPrice`/`maxPrice` bound the price, and `sort` is one of `name` (default), `-name`, `price` or " +
                "`-price`. `pageSize` is at most 100.")
            .Produces<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> HandleAsync(
        ISender sender,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sort = null)
    {
        var result = await sender
            .Send(new GetProductsQuery(pageNumber, pageSize, search, minPrice, maxPrice, sort), cancellationToken)
            .ConfigureAwait(false);

        return result.Match(products => TypedResults.Ok(products), error => error.ToProblem());
    }
}
