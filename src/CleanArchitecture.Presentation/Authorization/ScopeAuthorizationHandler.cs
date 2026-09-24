using Microsoft.AspNetCore.Authorization;

namespace CleanArchitecture.Presentation.Authorization;

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
