using CleanArchitecture.SharedKernel.Results;
using Shouldly;

namespace CleanArchitecture.SharedKernel.UnitTests.Results;

public class ErrorTests
{
    [Fact]
    public void None_HasEmptyCodeAndDescription()
    {
        Error.None.Code.ShouldBe(string.Empty);
        Error.None.Description.ShouldBe(string.Empty);
        Error.None.ErrorType.ShouldBe(ErrorType.Failure);
    }

    [Fact]
    public void NullValue_HasFailureType()
    {
        Error.NullValue.ErrorType.ShouldBe(ErrorType.Failure);
        Error.NullValue.Code.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Failure_CreatesErrorWithFailureType()
    {
        var error = Error.Failure("Code", "Description");

        error.Code.ShouldBe("Code");
        error.Description.ShouldBe("Description");
        error.ErrorType.ShouldBe(ErrorType.Failure);
    }

    [Fact]
    public void NotFound_CreatesErrorWithNotFoundType()
    {
        var error = Error.NotFound("Code", "Description");

        error.ErrorType.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void Validation_CreatesErrorWithValidationType()
    {
        var error = Error.Validation("Code", "Description");

        error.ErrorType.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Conflict_CreatesErrorWithConflictType()
    {
        var error = Error.Conflict("Code", "Description");

        error.ErrorType.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void Problem_CreatesErrorWithProblemType()
    {
        var error = Error.Problem("Code", "Description");

        error.ErrorType.ShouldBe(ErrorType.Problem);
    }

    [Fact]
    public void Unauthorized_CreatesErrorWithUnauthorizedType()
    {
        var error = Error.Unauthorized("Code", "Description");

        error.ErrorType.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public void TwoErrorsWithSameValues_AreEqual()
    {
        var first = Error.Failure("Code", "Description");
        var second = Error.Failure("Code", "Description");

        first.ShouldBe(second);
    }
}
