using CleanArchitecture.Presentation.OpenApi;

namespace CleanArchitecture.Presentation.Extensions;

public static class OpenApiExtensions
{
    /// <summary>Registers the OpenAPI document, including bearer security and 401/403 responses per operation.</summary>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenApi(options => options
            .AddDocumentTransformer<BearerSecuritySchemeTransformer>()
            .AddOperationTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }
}
