using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.Authorization;

public sealed class ProblemDetailsAuthorizationResultHandler(IProblemDetailsService problemDetailsService)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);

        if (!authorizeResult.Challenged && !authorizeResult.Forbidden)
        {
            return;
        }

        var status = context.Response.StatusCode;
        if (context.Response.HasStarted
            || status is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
        {
            return;
        }

        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Detail = status == StatusCodes.Status401Unauthorized
                    ? "A valid bearer token is required to access this resource."
                    : "The bearer token does not grant the permissions this resource requires.",
            },
        }).ConfigureAwait(false);
    }
}
