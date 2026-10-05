using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Presentation.Middleware;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception is DbUpdateConcurrencyException
            ? ConcurrencyConflict(httpContext, exception)
            : Unexpected(httpContext, exception);

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private ProblemDetails ConcurrencyConflict(HttpContext httpContext, Exception exception)
    {
        GlobalExceptionHandlerMessages.ConcurrencyConflict(logger, exception, httpContext.Request.Path);

        return new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Concurrency.Conflict",
            Detail = "The resource was modified by another request. Reload it and try again.",
            Instance = httpContext.Request.Path,
        };
    }

    private ProblemDetails Unexpected(HttpContext httpContext, Exception exception)
    {
        GlobalExceptionHandlerMessages.UnhandledException(logger, exception, httpContext.Request.Path);

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The server encountered an unexpected condition. Please try again later.",
            Instance = httpContext.Request.Path,
        };
    }
}

internal static partial class GlobalExceptionHandlerMessages
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing request {RequestPath}.")]
    public static partial void UnhandledException(ILogger logger, Exception exception, PathString requestPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concurrency conflict while processing request {RequestPath}.")]
    public static partial void ConcurrencyConflict(ILogger logger, Exception exception, PathString requestPath);
}
