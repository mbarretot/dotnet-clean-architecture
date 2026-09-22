using System.Security.Claims;
using CleanArchitecture.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CleanArchitecture.Infrastructure.Identity;

/// <summary><see cref="IHttpContextAccessor"/> lets this non-web library reach the ambient request; null outside a request, which <c>AuditableEntitySaveChangesInterceptor</c> handles with a system fallback.</summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
