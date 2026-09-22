using System.Linq.Expressions;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;
using FluentValidation;

namespace CleanArchitecture.Application.Behaviors;

/// <summary>Short-circuits with a failed <see cref="ValidationError"/> result instead of throwing.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly Func<Error, TResponse> CreateFailureResult = BuildFailureFactory();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(request, cancellationToken))).ConfigureAwait(false);

        var failures = validationResults
            .SelectMany(validationResult => validationResult.Errors)
            .Where(failure => failure is not null)
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .ToArray();

        if (failures.Length == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var validationError = ValidationError.FromResults(failures.Select(Result.Failure));

        return CreateFailureResult(validationError);
    }

    /// <summary>Compiles the failure factory once per closed <typeparamref name="TResponse"/>, avoiding per-call reflection.</summary>
    private static Func<Error, TResponse> BuildFailureFactory()
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return error => (TResponse)(object)Result.Failure(error);
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result).GetMethod(nameof(Result.Failure), genericParameterCount: 1, types: [typeof(Error)])!
            .MakeGenericMethod(valueType);

        var errorParameter = Expression.Parameter(typeof(Error), "error");
        var call = Expression.Call(failureMethod, errorParameter);
        var lambda = Expression.Lambda<Func<Error, TResponse>>(Expression.Convert(call, typeof(TResponse)), errorParameter);

        return lambda.Compile();
    }
}
