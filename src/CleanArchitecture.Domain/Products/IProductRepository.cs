namespace CleanArchitecture.Domain.Products;

/// <summary>Changes are committed separately, through <see cref="SharedKernel.Abstractions.IUnitOfWork"/>.</summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetAllAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);

    void Add(Product product);

    void Update(Product product);

    void Remove(Product product);
}
