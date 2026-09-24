using CleanArchitecture.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

/// <summary>
/// Always loads the whole aggregate (lines included) so its invariants can be enforced in memory. Mutating members only
/// stage changes; nothing persists until <see cref="SharedKernel.Abstractions.IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
public sealed class OrderRepository(ApplicationDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Orders
            .Include(order => order.Lines)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(
        string customerId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        await dbContext.Orders
            .Include(order => order.Lines)
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedAt)
            .ThenBy(order => order.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(Order order) => dbContext.Orders.Add(order);
}
