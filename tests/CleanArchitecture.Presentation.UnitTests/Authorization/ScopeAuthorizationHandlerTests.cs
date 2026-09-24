using System.Security.Claims;
using CleanArchitecture.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Shouldly;

namespace CleanArchitecture.Presentation.UnitTests.Authorization;

public class ScopeAuthorizationHandlerTests
{
    private static readonly ScopeRequirement ProductsWriteRequirement = new(Scopes.ProductsWrite);

    [Theory]
    [InlineData("products:write")]
    [InlineData("products:read products:write")]
    [InlineData("openid  products:write profile")]
    public async Task HandleAsync_WithSpaceDelimitedScopeClaimContainingTheScope_Succeeds(string scopeClaimValue)
    {
        var context = CreateContext(new Claim(Scopes.ClaimType, scopeClaimValue));

        await new ScopeAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithOneOfSeveralScopeClaimsMatching_Succeeds()
    {
        var context = CreateContext(
            new Claim(Scopes.ClaimType, "products:read"),
            new Claim(Scopes.ClaimType, "products:write"));

        await new ScopeAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("products:read")]
    [InlineData("products:writer")]
    [InlineData("Products:Write")]
    [InlineData("")]
    public async Task HandleAsync_WithoutAnExactScopeMatch_DoesNotSucceed(string scopeClaimValue)
    {
        var context = CreateContext(new Claim(Scopes.ClaimType, scopeClaimValue));

        await new ScopeAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithoutAnyScopeClaim_DoesNotSucceed()
    {
        var context = CreateContext(new Claim("sub", "user-1"));

        await new ScopeAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }

    private static AuthorizationHandlerContext CreateContext(params Claim[] claims) =>
        new([ProductsWriteRequirement], new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")), resource: null);
}
