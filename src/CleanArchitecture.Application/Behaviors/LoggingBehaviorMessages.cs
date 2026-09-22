using CleanArchitecture.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Behaviors;

internal static partial class LoggingBehaviorMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting request {RequestName}")]
    public static partial void RequestStarting(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request {RequestName} succeeded in {ElapsedMilliseconds}ms")]
    public static partial void RequestSucceeded(ILogger logger, string requestName, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request {RequestName} failed with {ErrorCode} ({ErrorType}) in {ElapsedMilliseconds}ms")]
    public static partial void RequestFailed(ILogger logger, string requestName, string errorCode, ErrorType errorType, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request {RequestName} threw an exception after {ElapsedMilliseconds}ms")]
    public static partial void RequestThrew(ILogger logger, Exception exception, string requestName, long elapsedMilliseconds);
}
