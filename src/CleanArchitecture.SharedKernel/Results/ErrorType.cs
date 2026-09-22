namespace CleanArchitecture.SharedKernel.Results;

/// <summary>Lets consumers (e.g. HTTP) map an error to the right response.</summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Problem = 5,
}
