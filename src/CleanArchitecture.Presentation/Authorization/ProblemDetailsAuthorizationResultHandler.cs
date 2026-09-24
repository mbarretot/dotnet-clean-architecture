using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.Authorization;

/// <summary>
/// Turns the empty 401/403 left by authentication handlers into RFC 9457 problem responses written by
/// <see cref="IProblemDetailsService"/>, so they carry the same <c>type</c>, <c>title</c> and <c>traceId</c> as every
/// other error. It wraps the default handler rather than hooking <c>JwtBearerEvents</c>: the authorization middleware
/// is the single place every policy outcome passes through (explicit policies and the fallback policy alike), it stays
/// scheme-agnostic, and letting the scheme challenge first keeps its <c>WWW-Authenticate</c> header (including
/// <c>error="invalid_token"</c>) intact. The body never includes the authentication failure, so token validation
/// details are not leaked.
/// </summary>
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

        // Only decorate a bare 401/403: a scheme that redirected or already wrote a body keeps its own response.
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
