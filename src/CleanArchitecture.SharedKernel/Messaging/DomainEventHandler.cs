namespace CleanArchitecture.SharedKernel.Messaging;

public abstract class DomainEventHandler<TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public abstract Task Handle(TDomainEvent notification, CancellationToken cancellationToken);
}
