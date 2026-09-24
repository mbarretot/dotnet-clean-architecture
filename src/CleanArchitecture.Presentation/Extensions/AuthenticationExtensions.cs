using CleanArchitecture.Presentation.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CleanArchitecture.Presentation.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.MapInboundClaims = false);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ScopeAuthorizationHandler>());
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AuthorizationPolicies.ProductsWrite, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ScopeRequirement(Scopes.ProductsWrite)))
            .AddPolicy(AuthorizationPolicies.OrdersWrite, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ScopeRequirement(Scopes.OrdersWrite)));

        return services;
    }
}
