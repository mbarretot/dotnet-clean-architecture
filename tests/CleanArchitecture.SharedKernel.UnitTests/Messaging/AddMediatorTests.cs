using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CleanArchitecture.SharedKernel.UnitTests.Messaging;

public class AddMediatorTests
{
    private sealed record TestCommand : ICommand;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public Task<Result> Handle(TestCommand request, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed record TestQuery : IQuery<int>;

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, int>
    {
        public Task<Result<int>> Handle(TestQuery request, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(1));
    }

    private sealed record TestNotification : INotification;

    private sealed class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        public Task Handle(TestNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public void AddMediator_RegistersSenderAndPublisher()
    {
        var services = new ServiceCollection();

        services.AddMediator(typeof(AddMediatorTests).Assembly);
        using var provider = services.BuildServiceProvider();

        provider.GetService<ISender>().ShouldNotBeNull();
        provider.GetService<IPublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void AddMediator_RegistersCommandHandlerUnderRequestHandlerInterface()
    {
        var services = new ServiceCollection();

        services.AddMediator(typeof(AddMediatorTests).Assembly);
        using var provider = services.BuildServiceProvider();

        provider.GetService<IRequestHandler<TestCommand, Result>>().ShouldBeOfType<TestCommandHandler>();
    }

    [Fact]
    public void AddMediator_RegistersQueryHandlerUnderRequestHandlerInterface()
    {
        var services = new ServiceCollection();

        services.AddMediator(typeof(AddMediatorTests).Assembly);
        using var provider = services.BuildServiceProvider();

        provider.GetService<IRequestHandler<TestQuery, Result<int>>>().ShouldBeOfType<TestQueryHandler>();
    }

    [Fact]
    public void AddMediator_RegistersNotificationHandler()
    {
        var services = new ServiceCollection();

        services.AddMediator(typeof(AddMediatorTests).Assembly);
        using var provider = services.BuildServiceProvider();

        provider.GetService<INotificationHandler<TestNotification>>().ShouldBeOfType<TestNotificationHandler>();
    }

    [Fact]
    public async Task AddMediator_EndToEnd_SenderDispatchesToScannedHandler()
    {
        var services = new ServiceCollection();

        services.AddMediator(typeof(AddMediatorTests).Assembly);
        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new TestCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }
}
