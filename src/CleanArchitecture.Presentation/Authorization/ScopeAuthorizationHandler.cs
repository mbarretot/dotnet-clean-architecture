using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Presentation.Authorization;

/// <summary>
/// Issuers disagree on the scope shape: most emit one space-delimited <c>scope</c> claim (RFC 8693),
/// while others (e.g. <c>dotnet user-jwts</c>) emit a JSON array that becomes one claim per scope. Both are accepted.
/// </summary>
public sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var hasScope = context.User.FindAll(Scopes.ClaimType)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(requirement.Scope, StringComparer.Ordinal);

        if (hasScope)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
