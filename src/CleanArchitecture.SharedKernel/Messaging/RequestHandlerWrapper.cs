using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.SharedKernel.Messaging;

internal abstract class RequestHandlerWrapperBase
{
    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapperBase
    where TRequest : IRequest<TResponse>
{
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        Task<TResponse> Handler()
        {
            var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
                ?? throw new InvalidOperationException(
                    $"No handler registered for request type '{typeof(TRequest).Name}'. " +
                    $"Ensure a class implementing IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}> is registered.");

            return handler.Handle(typedRequest, cancellationToken);
        }

        var pipeline = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .Aggregate(
                (RequestHandlerDelegate<TResponse>)Handler,
                (next, behavior) => () => behavior.Handle(typedRequest, next, cancellationToken));

        return await pipeline().ConfigureAwait(false);
    }
}
