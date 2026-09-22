namespace CleanArchitecture.SharedKernel.Results;

public record Error(string Code, string Description, ErrorType ErrorType)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.", ErrorType.Failure);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Problem(string code, string description) => new(code, description, ErrorType.Problem);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);
}
