using CleanArchitecture.SharedKernel.Messaging;
using CleanArchitecture.SharedKernel.Results;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CleanArchitecture.SharedKernel.UnitTests.Messaging;

public class SenderTests
{
    private sealed record Ping(string Message) : IRequest<Result<string>>;

    private sealed class PingHandler : IRequestHandler<Ping, Result<string>>
    {
        public Task<Result<string>> Handle(Ping request, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(request.Message));
    }

    private sealed record UnhandledRequest : IRequest<Result>;

    private static readonly List<string> ExecutionOrder = [];

    private sealed class RecordingBehaviorA : IPipelineBehavior<Ping, Result<string>>
    {
        public Task<Result<string>> Handle(Ping request, RequestHandlerDelegate<Result<string>> next, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add("A-before");
            var task = next();
            ExecutionOrder.Add("A-after");
            return task;
        }
    }

    private sealed class RecordingBehaviorB : IPipelineBehavior<Ping, Result<string>>
    {
        public Task<Result<string>> Handle(Ping request, RequestHandlerDelegate<Result<string>> next, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add("B-before");
            var task = next();
            ExecutionOrder.Add("B-after");
            return task;
        }
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<ISender, Sender>();
        services.AddScoped<IRequestHandler<Ping, Result<string>>, PingHandler>();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Send_DispatchesToRegisteredHandler()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send<Result<string>>(new Ping("hello"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }

    [Fact]
    public async Task Send_WithNoRegisteredHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddScoped<ISender, Sender>();
        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sender.Send<Result>(new UnhandledRequest(), TestContext.Current.CancellationToken));

        exception.Message.ShouldContain(nameof(UnhandledRequest));
    }

    [Fact]
    public async Task Send_AppliesPipelineBehaviorsInRegistrationOrder_FirstRegisteredIsOutermost()
    {
        ExecutionOrder.Clear();
        await using var provider = BuildProvider(services =>
        {
            services.AddScoped<IPipelineBehavior<Ping, Result<string>>, RecordingBehaviorA>();
            services.AddScoped<IPipelineBehavior<Ping, Result<string>>, RecordingBehaviorB>();
        });
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send<Result<string>>(new Ping("hello"), TestContext.Current.CancellationToken);

        ExecutionOrder.ShouldBe(["A-before", "B-before", "B-after", "A-after"]);
    }

    [Fact]
    public async Task Send_CachesHandlerWrapper_SoRepeatedCallsResolveSuccessfully()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();

        var first = await sender.Send<Result<string>>(new Ping("first"), TestContext.Current.CancellationToken);
        var second = await sender.Send<Result<string>>(new Ping("second"), TestContext.Current.CancellationToken);

        first.Value.ShouldBe("first");
        second.Value.ShouldBe("second");
    }
}
