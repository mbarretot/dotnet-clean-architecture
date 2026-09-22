using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shouldly;

namespace CleanArchitecture.Presentation.UnitTests.Extensions;

public class ResultExtensionsTests
{
    public static TheoryData<ErrorType, int> ErrorTypeToStatusCode => new()
    {
        { ErrorType.Validation, StatusCodes.Status400BadRequest },
        { ErrorType.NotFound, StatusCodes.Status404NotFound },
        { ErrorType.Conflict, StatusCodes.Status409Conflict },
        { ErrorType.Unauthorized, StatusCodes.Status401Unauthorized },
        { ErrorType.Problem, StatusCodes.Status400BadRequest },
        { ErrorType.Failure, StatusCodes.Status400BadRequest },
    };

    [Theory]
    [MemberData(nameof(ErrorTypeToStatusCode))]
    public void ToProblem_MapsEveryErrorTypeToItsExpectedStatusCode(ErrorType errorType, int expectedStatusCode)
    {
        var error = new Error("Some.Code", "Some description.", errorType);

        var result = error.ToProblem();

        var statusCodeResult = result.ShouldBeAssignableTo<IStatusCodeHttpResult>();
        statusCodeResult.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Fact]
    public void ToProblem_WithValidationError_ReturnsValidationProblemWithPerFieldErrors()
    {
        var validationError = ValidationError.FromResults([
            Result.Failure(Error.Validation("Name", "Name is required.")),
            Result.Failure(Error.Validation("Price", "Price must be non-negative.")),
        ]);

        var result = validationError.ToProblem();

        var validationProblemResult = result.ShouldBeOfType<ValidationProblem>();
        validationProblemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblemResult.ProblemDetails.Errors.Keys.ShouldBe(["Name", "Price"], ignoreOrder: true);
        validationProblemResult.ProblemDetails.Errors["Name"].ShouldContain("Name is required.");
    }

    [Fact]
    public void ToProblem_WithNonValidationError_UsesErrorCodeAndDescriptionAsTitleAndDetail()
    {
        var error = Error.NotFound("Product.NotFound", "The product was not found.");

        var result = error.ToProblem();

        var problemResult = result.ShouldBeOfType<ProblemHttpResult>();
        problemResult.ProblemDetails.Title.ShouldBe("Product.NotFound");
        problemResult.ProblemDetails.Detail.ShouldBe("The product was not found.");
    }

    [Fact]
    public void ToProblem_WithSuccessError_Throws() =>
        Should.Throw<InvalidOperationException>(() => Error.None.ToProblem());

    [Fact]
    public void ToOkResult_WithSuccessfulResult_ReturnsOk()
    {
        var result = Result.Success();

        var httpResult = result.ToOkResult();

        httpResult.ShouldBeAssignableTo<IStatusCodeHttpResult>()!.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public void ToOkResult_WithFailedResult_ReturnsProblem()
    {
        var result = Result.Failure(Error.Conflict("Product.AlreadyInactive", "Already inactive."));

        var httpResult = result.ToOkResult();

        httpResult.ShouldBeAssignableTo<IStatusCodeHttpResult>()!.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void ToNoContentResult_WithSuccessfulResult_ReturnsNoContent()
    {
        var result = Result.Success();

        var httpResult = result.ToNoContentResult();

        httpResult.ShouldBeAssignableTo<IStatusCodeHttpResult>()!.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void ToNoContentResult_WithFailedResult_ReturnsProblem()
    {
        var result = Result.Failure(Error.NotFound("Product.NotFound", "Not found."));

        var httpResult = result.ToNoContentResult();

        httpResult.ShouldBeAssignableTo<IStatusCodeHttpResult>()!.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
