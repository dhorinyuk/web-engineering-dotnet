using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class BrandRepository : GenericRepository<Brand>, IBrandRepository
{
    public BrandRepository(CatalogDbContext context) : base(context)
    {
    }

    public async Task<Brand?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await DbSet.FirstOrDefaultAsync(b => b.Code == normalized, ct);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = DbSet.Where(b => b.Code == normalized);
        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var query = DbSet.Where(b => b.Name.ToLower() == normalized);
        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }
}
