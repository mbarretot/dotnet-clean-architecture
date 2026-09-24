using System.Security.Claims;
using CleanArchitecture.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CleanArchitecture.Presentation.UnitTests.Authorization;

public sealed class FallbackPolicyTests
{
    [Fact]
    public async Task FallbackPolicy_IsConfiguredAndRequiresAnAuthenticatedUser()
    {
        await using var services = BuildServices();

        var fallbackPolicy = await services.GetRequiredService<IAuthorizationPolicyProvider>().GetFallbackPolicyAsync();

        fallbackPolicy.ShouldNotBeNull();
        fallbackPolicy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task FallbackPolicy_RejectsAnonymousUser()
    {
        var result = await AuthorizeAgainstFallbackAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task FallbackPolicy_AcceptsAnAuthenticatedUserWithoutScopes()
    {
        var authenticated = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "reader")], "Bearer"));

        var result = await AuthorizeAgainstFallbackAsync(authenticated);

        result.Succeeded.ShouldBeTrue();
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthenticationAndAuthorization();

        return services.BuildServiceProvider();
    }

    private static async Task<AuthorizationResult> AuthorizeAgainstFallbackAsync(ClaimsPrincipal user)
    {
        await using var services = BuildServices();
        var fallbackPolicy = await services.GetRequiredService<IAuthorizationPolicyProvider>().GetFallbackPolicyAsync();
        fallbackPolicy.ShouldNotBeNull();

        return await services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user, fallbackPolicy);
    }
}
