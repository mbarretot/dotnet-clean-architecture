namespace CleanArchitecture.SharedKernel.Results;

/// <summary>Outcome of an operation that avoids exceptions for control flow.</summary>
public class Result
{
    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>Wraps a null value as <see cref="Error.NullValue"/> instead of success.</summary>
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is null ? Failure<TValue>(Error.NullValue) : Success(value);

    public static Result Ensure(bool condition, Error error) => condition ? Success() : Failure(error);
}
