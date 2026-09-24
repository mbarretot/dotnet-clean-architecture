using System.Globalization;
using CleanArchitecture.Presentation.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CleanArchitecture.Presentation.OpenApi;

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IAuthorizationPolicyProvider authorizationPolicyProvider)
    : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string SchemeId = JwtBearerDefaults.AuthenticationScheme;

    private const string ProblemJsonContentType = "application/problem+json";

    private const string ProblemDetailsSchemaId = nameof(ProblemDetails);

    public async Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (await authenticationSchemeProvider.GetSchemeAsync(SchemeId).ConfigureAwait(false) is null)
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = $"JWT access token. Write operations require the `{Scopes.ProductsWrite}` scope.",
        };
    }

    public async Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var policy = await GetEffectivePolicyAsync(context.Description.ActionDescriptor.EndpointMetadata)
            .ConfigureAwait(false);

        if (policy is null)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeId, context.Document)] = [],
        });

        var problemSchema = await GetProblemDetailsSchemaReferenceAsync(context, cancellationToken).ConfigureAwait(false);

        operation.Responses ??= [];
        operation.Responses.TryAdd(
            StatusCodes.Status401Unauthorized.ToString(CultureInfo.InvariantCulture),
            ProblemResponse("Unauthorized: the bearer token is missing, expired or invalid.", problemSchema));

        if (policy.Requirements.Any(requirement => requirement is not DenyAnonymousAuthorizationRequirement))
        {
            operation.Responses.TryAdd(
                StatusCodes.Status403Forbidden.ToString(CultureInfo.InvariantCulture),
                ProblemResponse("Forbidden: the token lacks a required scope or role.", problemSchema));
        }
    }

    private static OpenApiResponse ProblemResponse(string description, IOpenApiSchema schema) => new()
    {
        Description = description,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            [ProblemJsonContentType] = new OpenApiMediaType { Schema = schema },
        },
    };

    private static async Task<IOpenApiSchema> GetProblemDetailsSchemaReferenceAsync(
        OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var document = context.Document!;
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        if (!document.Components.Schemas.ContainsKey(ProblemDetailsSchemaId))
        {
            var schema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            document.Components.Schemas[ProblemDetailsSchemaId] = schema;
        }

        return new OpenApiSchemaReference(ProblemDetailsSchemaId, document);
    }

    private async Task<AuthorizationPolicy?> GetEffectivePolicyAsync(IList<object> endpointMetadata)
    {
        if (endpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return null;
        }

        var explicitPolicy = await AuthorizationPolicy.CombineAsync(
                authorizationPolicyProvider,
                endpointMetadata.OfType<IAuthorizeData>(),
                endpointMetadata.OfType<AuthorizationPolicy>())
            .ConfigureAwait(false);

        return explicitPolicy ?? await authorizationPolicyProvider.GetFallbackPolicyAsync().ConfigureAwait(false);
    }
}
