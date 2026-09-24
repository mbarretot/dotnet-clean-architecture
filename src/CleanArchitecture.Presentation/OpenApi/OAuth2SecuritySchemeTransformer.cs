using CleanArchitecture.Presentation.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace CleanArchitecture.Presentation.OpenApi;

/// <summary>
/// When <see cref="OpenApiOAuth2Options"/> is configured, declares an OAuth 2.0 Authorization Code scheme and offers it
/// as an alternative to the bearer scheme on every protected operation. Both yield the same JWT; this one only tells
/// tooling (Scalar) how to obtain it. Must run after <see cref="BearerSecuritySchemeTransformer"/>.
/// </summary>
internal sealed class OAuth2SecuritySchemeTransformer(IOptions<OpenApiOAuth2Options> options)
    : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    public const string SchemeId = "OAuth2";

    private readonly OpenApiOAuth2Options _options = options.Value;

    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return Task.CompletedTask;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Sign in through the identity provider (Authorization Code + PKCE).",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(_options.AuthorizationUrl!),
                    TokenUrl = new Uri(_options.TokenUrl!),
                    Scopes = new Dictionary<string, string>
                    {
                        [Scopes.ProductsWrite] = "Create, update and delete products.",
                        [Scopes.OrdersWrite] = "Place and cancel your orders.",
                    },
                },
            },
        };

        return Task.CompletedTask;
    }

    public Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var isProtected = operation.Security?.Any(requirement =>
            requirement.Keys.Any(scheme => scheme.Reference?.Id == JwtBearerDefaults.AuthenticationScheme)) == true;

        if (!_options.IsConfigured || !isProtected)
        {
            return Task.CompletedTask;
        }

        // A second requirement object means "either scheme" (OpenAPI ORs the entries of the security array).
        operation.Security!.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeId, context.Document)] = [],
        });

        return Task.CompletedTask;
    }
}
