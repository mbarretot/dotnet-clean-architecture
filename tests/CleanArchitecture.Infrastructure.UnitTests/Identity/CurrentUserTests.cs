using System.Security.Claims;
using CleanArchitecture.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace CleanArchitecture.Infrastructure.UnitTests.Identity;

public class CurrentUserTests
{
    [Fact]
    public void UserId_WithUnmappedSubClaim_ReturnsSub()
    {
        var currentUser = CreateCurrentUser(new Claim("sub", "user-123"));

        currentUser.UserId.ShouldBe("user-123");
    }

    [Fact]
    public void UserId_WithMappedNameIdentifierClaim_ReturnsIt()
    {
        var currentUser = CreateCurrentUser(new Claim(ClaimTypes.NameIdentifier, "user-456"));

        currentUser.UserId.ShouldBe("user-456");
    }

    [Fact]
    public void UserId_WithBothClaims_PrefersSub()
    {
        var currentUser = CreateCurrentUser(
            new Claim(ClaimTypes.NameIdentifier, "mapped"),
            new Claim("sub", "raw"));

        currentUser.UserId.ShouldBe("raw");
    }

    [Fact]
    public void UserId_WithoutIdentifierClaims_ReturnsNull()
    {
        var currentUser = CreateCurrentUser(new Claim("scope", "products:write"));

        currentUser.UserId.ShouldBeNull();
    }

    [Fact]
    public void UserId_OutsideOfARequest_ReturnsNull()
    {
        var currentUser = new CurrentUser(new HttpContextAccessor());

        currentUser.UserId.ShouldBeNull();
    }

    private static CurrentUser CreateCurrentUser(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")) };

        return new CurrentUser(new HttpContextAccessor { HttpContext = httpContext });
    }
}
