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

    public static WebApplication MapApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDefaultEndpoints();

        app.MapEndpoints();

        app.MapOpenApi()
            .AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            var oauth2 = app.Services.GetRequiredService<IOptions<OpenApiOAuth2Options>>().Value;

            app.MapScalarApiReference(options => ConfigureSignIn(options, oauth2))
                .AllowAnonymous();
        }

        return app;
    }

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
                .WithSelectedScopes([Scopes.ProductsWrite, Scopes.OrdersWrite]));
    }
}
