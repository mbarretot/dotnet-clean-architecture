using CleanArchitecture.SharedKernel.Entities;
using CleanArchitecture.SharedKernel.Messaging;
using Shouldly;

namespace CleanArchitecture.SharedKernel.UnitTests.Entities;

public class AggregateRootTests
{
    private sealed record TestDomainEvent : IDomainEvent;

    private sealed class TestAggregate(Guid id) : AggregateRoot(id)
    {
        public void RaiseTestEvent() => Raise(new TestDomainEvent());
    }

    [Fact]
    public void NewAggregate_HasNoDomainEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Raise_AddsDomainEventToCollection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.RaiseTestEvent();

        aggregate.DomainEvents.ShouldHaveSingleItem();
        aggregate.DomainEvents[0].ShouldBeOfType<TestDomainEvent>();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseTestEvent();
        aggregate.RaiseTestEvent();

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.ShouldBeEmpty();
    }
}
