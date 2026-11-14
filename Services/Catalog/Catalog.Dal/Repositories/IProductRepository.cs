using Catalog.Domain.Entities;

namespace Catalog.Dal.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    // Specific Repo 1: Eager Loading - Loads product with all related graphs in a single SQL query (Criterion 6)
    Task<Product?> GetWithAllDetailsAsync(int id, CancellationToken ct = default);

    // Specific Repo 2: Explicit Loading - Loads root entity first, then explicitly loads images collection in separate query (Criterion 6)
    Task<Product?> GetWithExplicitImagesAsync(int id, CancellationToken ct = default);

    // Specific Repo 3: LINQ M:N query via junction entity filtering and sorting by own field 'AddedAt' (Criterion 6)
    Task<IReadOnlyList<Product>> GetProductsAssignedToCategorySinceAsync(int categoryId, DateTime since, CancellationToken ct = default);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null, CancellationToken ct = default);
    Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null, CancellationToken ct = default);
}
