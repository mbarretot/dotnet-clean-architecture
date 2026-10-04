using CleanArchitecture.Presentation.OpenApi;

namespace CleanArchitecture.Presentation.Extensions;

public static class OpenApiExtensions
{
    public const string DocumentTitle = "Clean Architecture API";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<OpenApiOAuth2Options>().BindConfiguration(OpenApiOAuth2Options.SectionName);

        // One document per API version (v1 today), each with the same transformers. Order matters: the OAuth2
        // transformer extends the bearer transformer's requirements.
        services.AddApiVersioning().AddOpenApi(options => options.Document
            .AddDocumentTransformer((document, _, _) =>
            {
                // Otherwise both come from the entry assembly, which is a different tool at build time.
                document.Info.Title = DocumentTitle;
                document.Info.Description =
                    "Select a version with the `api-version` query parameter or the `X-Api-Version` header; " +
                    "requests without one use 1.0.";
                return Task.CompletedTask;
            })
            .AddDocumentTransformer<BearerSecuritySchemeTransformer>()
            .AddOperationTransformer<BearerSecuritySchemeTransformer>()
            .AddDocumentTransformer<OAuth2SecuritySchemeTransformer>()
            .AddOperationTransformer<OAuth2SecuritySchemeTransformer>());

        return services;
    }
}
