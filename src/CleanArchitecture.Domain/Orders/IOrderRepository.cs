namespace CleanArchitecture.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByCustomerAsync(
        string customerId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    void Add(Order order);
}
