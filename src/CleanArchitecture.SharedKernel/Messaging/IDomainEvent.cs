namespace CleanArchitecture.SharedKernel.Messaging;

/// <summary>Raised by an aggregate root as a side effect of a state change.</summary>
public interface IDomainEvent : INotification;
