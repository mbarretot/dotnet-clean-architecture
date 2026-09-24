using CleanArchitecture.Presentation.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CleanArchitecture.Presentation.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Provider-agnostic JWT bearer: options bind from <c>Authentication:Schemes:Bearer</c> (the section
    /// <c>dotnet user-jwts</c> writes), so any OIDC issuer is plugged in through configuration alone. With no issuer
    /// configured the app still starts and every protected request is simply rejected with 401.
    /// </summary>
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            // Keep raw JWT claim names (sub, scope) instead of the legacy WS-Federation URIs.
            .AddJwtBearer(options => options.MapInboundClaims = false);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ScopeAuthorizationHandler>());
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();

        services.AddAuthorizationBuilder()
            // Secure by default: any endpoint without authorization metadata requires an authenticated user.
            // Public routes must opt out explicitly with AllowAnonymous(), pinned by AnonymousEndpointConventionTests.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AuthorizationPolicies.ProductsWrite, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ScopeRequirement(Scopes.ProductsWrite)));

        return services;
    }
}
