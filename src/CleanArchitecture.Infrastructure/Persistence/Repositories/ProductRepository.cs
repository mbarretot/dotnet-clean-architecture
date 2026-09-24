using CleanArchitecture.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetAllAsync(
        int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        await dbContext.Products
            .OrderBy(product => product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        dbContext.Products.AnyAsync(product => product.Sku == Sku.Create(sku).Value, cancellationToken);

    public void Add(Product product) => dbContext.Products.Add(product);

    public void Update(Product product) => dbContext.Products.Update(product);

    public void Remove(Product product) => dbContext.Products.Remove(product);
}
