using Catalog.Domain.Entities;

namespace Catalog.Dal.Repositories;

public interface IBrandRepository : IGenericRepository<Brand>
{
    Task<Brand?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);
}
