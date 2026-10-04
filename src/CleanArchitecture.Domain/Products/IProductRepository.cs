namespace CleanArchitecture.Domain.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> SearchAsync(ProductSearchCriteria criteria, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);

    void Add(Product product);

    void Update(Product product);

    void Remove(Product product);
}
