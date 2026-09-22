namespace CleanArchitecture.SharedKernel.Results;

public static class ResultExtensions
{
    public static TOut Match<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);

    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map) =>
        result.IsSuccess ? Result.Success(map(result.Value)) : Result.Failure<TOut>(result.Error);

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bind) =>
        result.IsSuccess ? bind(result.Value) : Result.Failure<TOut>(result.Error);

    public static Result<TIn> Ensure<TIn>(this Result<TIn> result, Func<TIn, bool> predicate, Error error)
    {
        if (result.IsFailure)
        {
            return result;
        }

        return predicate(result.Value) ? result : Result.Failure<TIn>(error);
    }
}
