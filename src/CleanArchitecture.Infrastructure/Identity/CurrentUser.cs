using System.Security.Claims;
using CleanArchitecture.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CleanArchitecture.Infrastructure.Identity;

/// <summary><see cref="IHttpContextAccessor"/> lets this non-web library reach the ambient request; null outside a request, which <c>AuditableEntitySaveChangesInterceptor</c> handles with a system fallback.</summary>
/// <remarks>
/// Reads the raw JWT <c>sub</c> claim first because JwtBearer is configured with inbound claim mapping disabled;
/// <see cref="ClaimTypes.NameIdentifier"/> is kept as a fallback for schemes that still map <c>sub</c> to it.
/// </remarks>
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
