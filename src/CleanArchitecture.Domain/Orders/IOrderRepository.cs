namespace CleanArchitecture.Domain.Orders;

/// <summary>
/// Loads and adds whole <see cref="Order"/> aggregates, lines included. Loaded orders are change-tracked; changes are
/// committed separately, through <see cref="SharedKernel.Abstractions.IUnitOfWork"/>.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a page of the customer's orders, newest first.</summary>
    Task<IReadOnlyList<Order>> GetByCustomerAsync(
        string customerId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    void Add(Order order);
}
