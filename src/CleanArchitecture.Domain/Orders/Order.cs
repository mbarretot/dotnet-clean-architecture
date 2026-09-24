using CleanArchitecture.Domain.Orders.Events;
using CleanArchitecture.Domain.Products;
using CleanArchitecture.SharedKernel.Entities;
using CleanArchitecture.SharedKernel.Results;

namespace CleanArchitecture.Domain.Orders;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderLine> _lines = [];

    private Order(Guid id, string customerId)
        : base(id)
    {
        CustomerId = customerId;
        Status = OrderStatus.Placed;
    }

    private Order()
    {
        CustomerId = string.Empty;
    }

    public string CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public Money Total =>
        Money.Create(_lines.Sum(line => line.LineTotal.Amount), _lines[0].UnitPrice.Currency).Value;

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
