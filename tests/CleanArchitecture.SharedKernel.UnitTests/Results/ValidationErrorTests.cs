using CleanArchitecture.SharedKernel.Results;
using Shouldly;

namespace CleanArchitecture.SharedKernel.UnitTests.Results;

public class ValidationErrorTests
{
    [Fact]
    public void FromResults_WithFailedResults_CollectsTheirErrors()
    {
        var firstError = Error.Validation("First", "First description");
        var secondError = Error.Validation("Second", "Second description");

        var results = new[]
        {
            Result.Failure(firstError),
            Result.Success(),
            Result.Failure(secondError),
        };

        var validationError = ValidationError.FromResults(results);

        validationError.Errors.ShouldBe([firstError, secondError]);
        validationError.ErrorType.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void FromResults_WithNoFailures_ReturnsEmptyErrors()
    {
        var results = new[] { Result.Success(), Result.Success() };

        var validationError = ValidationError.FromResults(results);

        validationError.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidationError_IsAssignableToError()
    {
        var validationError = ValidationError.FromResults([Result.Failure(Error.Validation("Code", "Description"))]);

        (validationError is Error).ShouldBeTrue();
    }
}
