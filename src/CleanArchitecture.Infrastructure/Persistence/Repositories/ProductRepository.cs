using CleanArchitecture.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> SearchAsync(
        ProductSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            // Translated to SQL lower(...) LIKE on PostgreSQL and SQLite alike (ILIKE is PostgreSQL-only), so the
            // culture and StringComparison overloads the analyzers ask for do not apply.
            var term = criteria.Search.Trim().ToLowerInvariant();
#pragma warning disable CA1304, CA1311, CA1862
            query = query.Where(product =>
                product.Name.ToLower().Contains(term) || product.Description.ToLower().Contains(term));
#pragma warning restore CA1304, CA1311, CA1862
        }

        if (criteria.MinPrice is { } minPrice)
        {
            query = query.Where(product => product.Price.Amount >= minPrice);
        }

        if (criteria.MaxPrice is { } maxPrice)
        {
            query = query.Where(product => product.Price.Amount <= maxPrice);
        }

        query = criteria.SortOrder switch
        {
            ProductSortOrder.NameDescending => query.OrderByDescending(product => product.Name),
            ProductSortOrder.PriceAscending => query.OrderBy(product => product.Price.Amount).ThenBy(product => product.Name),
            ProductSortOrder.PriceDescending => query.OrderByDescending(product => product.Price.Amount).ThenBy(product => product.Name),
            _ => query.OrderBy(product => product.Name),
        };

        return await ((IOrderedQueryable<Product>)query)
            .ThenBy(product => product.Id)
            .Skip((criteria.PageNumber - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        dbContext.Products.AnyAsync(product => product.Sku == Sku.Create(sku).Value, cancellationToken);

    public void Add(Product product) => dbContext.Products.Add(product);

    public void Update(Product product) => dbContext.Products.Update(product);

    public void Remove(Product product) => dbContext.Products.Remove(product);
}
