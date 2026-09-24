using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.SharedKernel.Messaging;

public sealed class Publisher(IServiceProvider serviceProvider) : IPublisher
{
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();

        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                exceptions ??= [];
                exceptions.Add(exception);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException("One or more notification handlers failed.", exceptions);
        }
    }
}
