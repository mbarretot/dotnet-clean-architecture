using System.Collections.Concurrent;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.SharedKernel.Messaging;

/// <summary>Caches the resolved wrapper per request type so only the first dispatch pays for reflection.</summary>
public sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapperBase> Wrappers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        var wrapper = Wrappers.GetOrAdd(requestType, type =>
        {
            var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(type, typeof(TResponse));
            return (RequestHandlerWrapperBase)Activator.CreateInstance(wrapperType)!;
        });

        return HandleAsync<TResponse>(wrapper, request, cancellationToken);
    }

    public Task<Result> Send(ICommand command, CancellationToken cancellationToken = default) =>
        Send<Result>(command, cancellationToken);

    private async Task<TResponse> HandleAsync<TResponse>(RequestHandlerWrapperBase wrapper, object request, CancellationToken cancellationToken)
    {
        var result = await wrapper.Handle(request, serviceProvider, cancellationToken).ConfigureAwait(false);
        return (TResponse)result!;
    }
}
