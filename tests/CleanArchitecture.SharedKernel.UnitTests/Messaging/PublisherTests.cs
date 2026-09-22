using CleanArchitecture.SharedKernel.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CleanArchitecture.SharedKernel.UnitTests.Messaging;

public class PublisherTests
{
    private sealed record TestNotification : INotification;

    private sealed class FirstHandler(List<string> calls) : INotificationHandler<TestNotification>
    {
        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            calls.Add(nameof(FirstHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler(List<string> calls) : INotificationHandler<TestNotification>
    {
        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            calls.Add(nameof(SecondHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : INotificationHandler<TestNotification>
    {
        public Task Handle(TestNotification notification, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("First handler failure.");
    }

    private sealed class OtherThrowingHandler : INotificationHandler<TestNotification>
    {
        public Task Handle(TestNotification notification, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Second handler failure.");
    }

    [Fact]
    public async Task Publish_InvokesAllRegisteredHandlers()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(calls);
        services.AddScoped<IPublisher, Publisher>();
        services.AddScoped<INotificationHandler<TestNotification>, FirstHandler>();
        services.AddScoped<INotificationHandler<TestNotification>, SecondHandler>();
        await using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IPublisher>();

        await publisher.Publish(new TestNotification(), TestContext.Current.CancellationToken);

        calls.ShouldBe([nameof(FirstHandler), nameof(SecondHandler)]);
    }

    [Fact]
    public async Task Publish_WithNoHandlers_CompletesSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPublisher, Publisher>();
        await using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IPublisher>();

        await Should.NotThrowAsync(async () =>
            await publisher.Publish(new TestNotification(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Publish_WhenHandlersThrow_AggregatesExceptions()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPublisher, Publisher>();
        services.AddScoped<INotificationHandler<TestNotification>, ThrowingHandler>();
        services.AddScoped<INotificationHandler<TestNotification>, OtherThrowingHandler>();
        await using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IPublisher>();

        var exception = await Should.ThrowAsync<AggregateException>(async () =>
            await publisher.Publish(new TestNotification(), TestContext.Current.CancellationToken));

        exception.InnerExceptions.Count.ShouldBe(2);
    }
}
