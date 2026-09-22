using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Presentation.Extensions;

public static class ResultExtensions
{
    /// <summary>Only call for a failed result; a successful one has no error to translate.</summary>
    public static IResult ToProblem(this Error error)
    {
        if (error == Error.None)
        {
            throw new InvalidOperationException("Cannot map a successful result's error to a problem response.");
        }

        if (error is ValidationError validationError)
        {
            return ToValidationProblem(validationError);
        }

        // TypedResults, not Results, so the concrete type is ProblemHttpResult: richer OpenAPI metadata and testability.
        return TypedResults.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: ToStatusCode(error.ErrorType));
    }

    /// <summary>200 OK on success, or a problem response on failure.</summary>
    public static IResult ToOkResult(this Result result) =>
        result.IsSuccess ? Results.Ok() : result.Error.ToProblem();

    /// <summary>204 No Content on success, or a problem response on failure.</summary>
    public static IResult ToNoContentResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();

    private static int ToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Problem => StatusCodes.Status400BadRequest,
        ErrorType.Failure => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static Microsoft.AspNetCore.Http.HttpResults.ValidationProblem ToValidationProblem(ValidationError validationError)
    {
        var errors = validationError.Errors
            .GroupBy(fieldError => fieldError.Code, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(fieldError => fieldError.Description).ToArray(),
                StringComparer.Ordinal);

        return TypedResults.ValidationProblem(errors);
    }
}
