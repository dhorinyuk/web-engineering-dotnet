using Catalog.Domain.Entities;

namespace Catalog.Dal.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Category?> GetWithSubCategoriesAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetRootCategoriesAsync(CancellationToken ct = default);
    Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null, CancellationToken ct = default);
}
