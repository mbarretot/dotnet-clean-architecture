using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Presentation.Extensions;

public static class ResultExtensions
{
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

        return TypedResults.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: ToStatusCode(error.ErrorType));
    }

    public static IResult ToOkResult(this Result result) =>
        result.IsSuccess ? Results.Ok() : result.Error.ToProblem();

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
