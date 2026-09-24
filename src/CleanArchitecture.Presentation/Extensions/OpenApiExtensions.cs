using CleanArchitecture.Presentation.OpenApi;

namespace CleanArchitecture.Presentation.Extensions;

public static class OpenApiExtensions
{
    /// <summary>
    /// Registers the OpenAPI document, including bearer security and 401/403 responses per operation, plus the
    /// optional OAuth 2.0 sign-in flow bound from <see cref="OpenApiOAuth2Options.SectionName"/>.
    /// </summary>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<OpenApiOAuth2Options>().BindConfiguration(OpenApiOAuth2Options.SectionName);

        // Order matters: the OAuth2 transformer extends the security requirements the bearer transformer adds.
        services.AddOpenApi(options => options
            .AddDocumentTransformer<BearerSecuritySchemeTransformer>()
            .AddOperationTransformer<BearerSecuritySchemeTransformer>()
            .AddDocumentTransformer<OAuth2SecuritySchemeTransformer>()
            .AddOperationTransformer<OAuth2SecuritySchemeTransformer>());

        return services;
    }
}
