using CleanArchitecture.Domain.Orders.Events;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Entities;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Domain.Orders;

/// <summary>
/// Aggregate root owning its <see cref="OrderLine"/>s. Lines are only created through <see cref="Place"/>, so every
/// order that exists has at least one line, positive quantities and a single currency. Products are referenced by id
/// only; coordinating the two aggregates is the application layer's job.
/// </summary>
public sealed class Order : AggregateRoot
{
    private readonly List<OrderLine> _lines = [];

    private Order(Guid id, string customerId)
        : base(id)
    {
        CustomerId = customerId;
        Status = OrderStatus.Placed;
    }

    /// <summary>For EF Core only.</summary>
    private Order()
    {
        CustomerId = string.Empty;
    }

    /// <summary>The <c>sub</c> of the caller who placed the order.</summary>
    public string CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>Always defined: an order has at least one line, and all lines share one currency.</summary>
    public Money Total =>
        Money.Create(_lines.Sum(line => line.LineTotal.Amount), _lines[0].UnitPrice.Currency).Value;

    /// <summary>Raises <see cref="OrderPlacedDomainEvent"/>. Lines for the same product are merged into one.</summary>
    public static Result<Order> Place(string customerId, IReadOnlyCollection<OrderLineDraft> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Result.Failure<Order>(OrderErrors.CustomerRequired);
        }

        if (lines.Count == 0)
        {
            return Result.Failure<Order>(OrderErrors.NoLines);
        }

        if (lines.Any(line => line.Quantity <= 0))
        {
            return Result.Failure<Order>(OrderErrors.QuantityNotPositive);
        }

        if (lines.Select(line => line.UnitPrice.Currency).Distinct(StringComparer.Ordinal).Skip(1).Any())
        {
            return Result.Failure<Order>(OrderErrors.CurrencyMismatch);
        }

        var order = new Order(Guid.NewGuid(), customerId.Trim());

        foreach (var line in lines)
        {
            order.AddLine(line);
        }

        order.Raise(new OrderPlacedDomainEvent(order.Id));

        return order;
    }

    /// <summary>Raises <see cref="OrderCancelledDomainEvent"/>; a cancelled order cannot be cancelled again.</summary>
    public Result Cancel()
    {
        if (Status == OrderStatus.Cancelled)
        {
            return Result.Failure(OrderErrors.AlreadyCancelled);
        }

        Status = OrderStatus.Cancelled;

        Raise(new OrderCancelledDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>A repeated product keeps its first snapshot and adds up the quantities.</summary>
    private void AddLine(OrderLineDraft draft)
    {
        var existing = _lines.Find(line => line.ProductId == draft.ProductId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(draft.Quantity);
            return;
        }

        _lines.Add(OrderLine.Create(draft));
    }
}
