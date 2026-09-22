using CleanArchitecture.Application.Behaviors;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;
using FluentValidation;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record TestCommand(string Name) : ICommand;

    private sealed record TestQuery(string Name) : IQuery<string>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator() => RuleFor(command => command.Name).NotEmpty();
    }

    private sealed class TestCommandMinimumLengthValidator : AbstractValidator<TestCommand>
    {
        public TestCommandMinimumLengthValidator() => RuleFor(command => command.Name).MinimumLength(10);
    }

    private sealed class TestQueryValidator : AbstractValidator<TestQuery>
    {
        public TestQueryValidator() => RuleFor(query => query.Name).NotEmpty();
    }

    private sealed class NextSpy<TResponse>(TResponse response)
    {
        public int InvocationCount { get; private set; }

        public Task<TResponse> Invoke()
        {
            InvocationCount++;
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task Handle_WithNoValidators_InvokesNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([]);
        var next = new NextSpy<Result>(Result.Success());

        var result = await behavior.Handle(new TestCommand("valid"), next.Invoke, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        next.InvocationCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithValidRequest_InvokesNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator()]);
        var next = new NextSpy<Result>(Result.Success());

        var result = await behavior.Handle(new TestCommand("valid"), next.Invoke, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        next.InvocationCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithInvalidCommand_ReturnsFailureResultWithoutInvokingNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator()]);
        var next = new NextSpy<Result>(Result.Success());

        var result = await behavior.Handle(new TestCommand(string.Empty), next.Invoke, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        next.InvocationCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithInvalidQuery_ReturnsFailureResultOfTWithoutInvokingNext()
    {
        var behavior = new ValidationBehavior<TestQuery, Result<string>>([new TestQueryValidator()]);
        var next = new NextSpy<Result<string>>(Result.Success("value"));

        var result = await behavior.Handle(new TestQuery(string.Empty), next.Invoke, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        next.InvocationCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithMultipleFailingValidators_CollectsAllErrors()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>(
            [new TestCommandValidator(), new TestCommandMinimumLengthValidator()]);
        var next = new NextSpy<Result>(Result.Success());

        var result = await behavior.Handle(new TestCommand(string.Empty), next.Invoke, TestContext.Current.CancellationToken);

        var validationError = result.Error.ShouldBeOfType<ValidationError>();
        validationError.Errors.Length.ShouldBeGreaterThanOrEqualTo(1);
    }
}
