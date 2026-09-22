using CleanArchitecture.Application.Behaviors;
using CleanArchitecture.Application.UnitTests.TestDoubles;
using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace CleanArchitecture.Application.UnitTests.Behaviors;

public class LoggingBehaviorTests
{
    private sealed record TestCommand : ICommand;

    [Fact]
    public async Task Handle_OnSuccess_LogsStartAndSuccess()
    {
        var logger = new FakeLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(logger);

        var result = await behavior.Handle(
            new TestCommand(),
            () => Task.FromResult(Result.Success()),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Information && entry.Message.Contains("Starting"));
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Information && entry.Message.Contains("succeeded"));
    }

    [Fact]
    public async Task Handle_OnFailure_LogsWarningWithErrorDetails()
    {
        var logger = new FakeLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(logger);
        var error = Error.Failure("Some.Error", "Something went wrong.");

        var result = await behavior.Handle(
            new TestCommand(),
            () => Task.FromResult(Result.Failure(error)),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains(error.Code));
    }

    [Fact]
    public async Task Handle_WhenNextThrows_LogsErrorAndRethrows()
    {
        var logger = new FakeLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(logger);
        var thrown = new InvalidOperationException("boom");

        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await behavior.Handle(
                new TestCommand(),
                () => throw thrown,
                TestContext.Current.CancellationToken));

        exception.ShouldBeSameAs(thrown);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error && entry.Exception == thrown);
    }

    [Fact]
    public async Task Handle_NeverLogsRequestPayload()
    {
        var logger = new FakeLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(logger);

        await behavior.Handle(new TestCommand(), () => Task.FromResult(Result.Success()), TestContext.Current.CancellationToken);

        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(new TestCommand().ToString()!));
    }
}
