using System.Reflection;
using CleanArchitecture.Presentation.Authorization;
using CleanArchitecture.Presentation.Endpoints;
using CleanArchitecture.Presentation.OpenApi;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace CleanArchitecture.Presentation.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var descriptors = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type));

        services.TryAddEnumerable(descriptors);

        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        foreach (var endpoint in app.Services.GetServices<IEndpoint>())
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }

    /// <summary>
    /// Maps the whole HTTP surface. Everything is protected by the authorization fallback policy unless it opts out
    /// with <c>AllowAnonymous()</c>; the anonymous routes are pinned by a convention test.
    /// </summary>
    public static WebApplication MapApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Health probes are anonymous (see MapDefaultEndpoints): the platform calls them without credentials.
        app.MapDefaultEndpoints();

        // Feature endpoints: explicit policies where they add intent, the fallback policy for everything else.
        app.MapEndpoints();

        // Public: API clients and tooling need the contract before they can obtain a token. It lists operations and
        // their security requirements, never data.
        app.MapOpenApi()
            .AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            // Public (Development only): the reference UI must load so a developer can paste a bearer token into it,
            // or sign in through the identity provider when an OAuth 2.0 flow is configured.
            var oauth2 = app.Services.GetRequiredService<IOptions<OpenApiOAuth2Options>>().Value;

            app.MapScalarApiReference(options => ConfigureSignIn(options, oauth2))
                .AllowAnonymous();
        }

        return app;
    }

    /// <summary>Pre-selects the OAuth 2.0 scheme (Authorization Code + PKCE) when configured; otherwise a no-op.</summary>
    private static void ConfigureSignIn(ScalarOptions options, OpenApiOAuth2Options oauth2)
    {
        if (!oauth2.IsConfigured)
        {
            return;
        }

        options
            .AddPreferredSecuritySchemes(OAuth2SecuritySchemeTransformer.SchemeId)
            .AddAuthorizationCodeFlow(OAuth2SecuritySchemeTransformer.SchemeId, flow => flow
                .WithClientId(oauth2.ClientId!)
                .WithPkce(Pkce.Sha256)
                .WithSelectedScopes([Scopes.ProductsWrite]));
    }
}
