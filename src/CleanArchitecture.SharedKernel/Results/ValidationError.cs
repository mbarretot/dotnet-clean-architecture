namespace CleanArchitecture.SharedKernel.Results;

/// <summary>Aggregates all failed results' errors into one.</summary>
public sealed record ValidationError : Error
{
    private ValidationError(Error[] errors)
        : base("Validation.General", "One or more validation errors occurred.", ErrorType.Validation)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationError FromResults(IEnumerable<Result> results) =>
        new(results.Where(result => result.IsFailure).Select(result => result.Error).ToArray());
}
