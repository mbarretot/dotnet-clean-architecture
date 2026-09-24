using System.Collections.Concurrent;
using System.Reflection;
using CleanArchitecture.SharedKernel.Entities;
using CleanArchitecture.SharedKernel.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CleanArchitecture.Infrastructure.Persistence.Interceptors;

public sealed class DispatchDomainEventsInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> PublishMethods = new();

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await PublishDomainEventsAsync(eventData.Context, cancellationToken).ConfigureAwait(false);

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishDomainEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        var aggregatesWithEvents = context.ChangeTracker.Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        if (aggregatesWithEvents.Count == 0)
        {
            return;
        }

        var domainEvents = aggregatesWithEvents.SelectMany(aggregate => aggregate.DomainEvents).ToList();

        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            await PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var method = PublishMethods.GetOrAdd(
            domainEvent.GetType(),
            eventType => typeof(IPublisher).GetMethod(nameof(IPublisher.Publish))!.MakeGenericMethod(eventType));

        return (Task)method.Invoke(publisher, [domainEvent, cancellationToken])!;
    }
}
