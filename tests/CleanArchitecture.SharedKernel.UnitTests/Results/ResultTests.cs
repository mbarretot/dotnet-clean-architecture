using CleanArchitecture.SharedKernel.Results;
using Shouldly;

namespace CleanArchitecture.SharedKernel.UnitTests.Results;

public class ResultTests
{
    [Fact]
    public void Success_ReturnsSuccessfulResultWithNoError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_ReturnsFailedResultWithError()
    {
        var error = Error.Failure("Code", "Description");

        var result = Result.Failure(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void GenericFailure_AccessingValue_Throws()
    {
        var result = Result.Failure<int>(Error.Failure("Code", "Description"));

        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        Result<string> result = "hello";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void ImplicitConversion_FromNullValue_CreatesFailureResultWithNullValueError()
    {
        string? value = null;
        Result<string> result = value!;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NullValue);
    }

    [Fact]
    public void Ensure_ConditionTrue_ReturnsSuccess()
    {
        var result = Result.Ensure(true, Error.Failure("Code", "Description"));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Ensure_ConditionFalse_ReturnsFailure()
    {
        var error = Error.Failure("Code", "Description");

        var result = Result.Ensure(false, error);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Match_OnSuccess_InvokesSuccessBranch()
    {
        var result = Result.Success(10);

        var output = result.Match(value => value * 2, _ => -1);

        output.ShouldBe(20);
    }

    [Fact]
    public void Match_OnFailure_InvokesFailureBranch()
    {
        var error = Error.Failure("Code", "Description");
        var result = Result.Failure<int>(error);

        var output = result.Match(value => value, e => e == error ? -1 : -2);

        output.ShouldBe(-1);
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        var result = Result.Success(2);

        var mapped = result.Map(value => value * 10);

        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(20);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        var error = Error.Failure("Code", "Description");
        var result = Result.Failure<int>(error);

        var mapped = result.Map(value => value * 10);

        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldBe(error);
    }

    [Fact]
    public void Bind_OnSuccess_ChainsResult()
    {
        var result = Result.Success(2);

        var bound = result.Bind(value => Result.Success(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe("2");
    }

    [Fact]
    public void Bind_OnFailure_PropagatesError()
    {
        var error = Error.Failure("Code", "Description");
        var result = Result.Failure<int>(error);

        var bound = result.Bind(value => Result.Success(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        bound.IsFailure.ShouldBeTrue();
        bound.Error.ShouldBe(error);
    }

    [Fact]
    public void EnsurePredicate_OnSuccessMatchingPredicate_ReturnsSameResult()
    {
        var result = Result.Success(4);

        var ensured = result.Ensure(value => value > 0, Error.Failure("Code", "Description"));

        ensured.IsSuccess.ShouldBeTrue();
        ensured.Value.ShouldBe(4);
    }

    [Fact]
    public void EnsurePredicate_OnSuccessFailingPredicate_ReturnsFailure()
    {
        var error = Error.Failure("Code", "Description");
        var result = Result.Success(-4);

        var ensured = result.Ensure(value => value > 0, error);

        ensured.IsFailure.ShouldBeTrue();
        ensured.Error.ShouldBe(error);
    }
}
