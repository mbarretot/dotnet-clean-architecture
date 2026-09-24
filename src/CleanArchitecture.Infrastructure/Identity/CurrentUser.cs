using System.Security.Claims;
using CleanArchitecture.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CleanArchitecture.Infrastructure.Identity;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private const string SubjectClaimType = "sub";

    public string? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;

            return user?.FindFirstValue(SubjectClaimType) ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
