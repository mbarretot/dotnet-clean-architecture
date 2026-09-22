using System.Diagnostics;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Behaviors;

/// <summary>Logs only the request's type name, never its payload, which may contain personal data.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        LoggingBehaviorMessages.RequestStarting(logger, requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);

            stopwatch.Stop();

            if (response.IsSuccess)
            {
                LoggingBehaviorMessages.RequestSucceeded(logger, requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                LoggingBehaviorMessages.RequestFailed(
                    logger, requestName, response.Error.Code, response.Error.ErrorType, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            LoggingBehaviorMessages.RequestThrew(logger, exception, requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
