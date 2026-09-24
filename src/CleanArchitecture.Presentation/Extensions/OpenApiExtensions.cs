using CleanArchitecture.Presentation.OpenApi;

namespace CleanArchitecture.Presentation.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<OpenApiOAuth2Options>().BindConfiguration(OpenApiOAuth2Options.SectionName);

        // Order matters: the OAuth2 transformer extends the bearer transformer's requirements.
        services.AddOpenApi(options => options
            .AddDocumentTransformer<BearerSecuritySchemeTransformer>()
            .AddOperationTransformer<BearerSecuritySchemeTransformer>()
            .AddDocumentTransformer<OAuth2SecuritySchemeTransformer>()
            .AddOperationTransformer<OAuth2SecuritySchemeTransformer>());

        return services;
    }
}
