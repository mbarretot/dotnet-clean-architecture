using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.Middleware;

/// <summary>Logs the full exception server-side; the client only ever gets a generic 500 problem response.</summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        GlobalExceptionHandlerMessages.UnhandledException(logger, exception, httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The server encountered an unexpected condition. Please try again later.",
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);

        return true;
    }
}

internal static partial class GlobalExceptionHandlerMessages
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing request {RequestPath}.")]
    public static partial void UnhandledException(ILogger logger, Exception exception, PathString requestPath);
}
