using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(CatalogDbContext context) : base(context)
    {
    }

    // Specific Repo 1: Eager Loading (Criterion 6)
    // Loads entity and all related aggregates in a SINGLE SQL query via Include & ThenInclude
    public async Task<Product?> GetWithAllDetailsAsync(int id, CancellationToken ct = default)
    {
        return await DbSet
            .Include(p => p.Brand)
            .Include(p => p.Details)
            .Include(p => p.Images)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    // Specific Repo 2: Explicit Loading (Criterion 6)
    // First query retrieves root product; second separate call explicitly loads images collection
    public async Task<Product?> GetWithExplicitImagesAsync(int id, CancellationToken ct = default)
    {
        var product = await DbSet
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product != null)
        {
            // Explicit collection loading via DbContext Entry API
            await Context.Entry(product)
                .Collection(e => e.Images)
                .Query()
                .OrderBy(i => i.DisplayOrder)
                .LoadAsync(ct);
        }

        return product;
    }

    // Specific Repo 3: LINQ to Entities M:N Query (Criterion 6)
    // Query traverses explicit junction entity, filters and sorts by its own payload property (AddedAt)
    public async Task<IReadOnlyList<Product>> GetProductsAssignedToCategorySinceAsync(
        int categoryId,
        DateTime since,
        CancellationToken ct = default)
    {
        return await Context.ProductCategories
            .AsNoTracking()
            .Where(pc => pc.CategoryId == categoryId && pc.AddedAt >= since)
            .OrderByDescending(pc => pc.AddedAt)
            .Select(pc => pc.Product)
            .Include(p => p.Brand)
            .Include(p => p.Details)
            .ToListAsync(ct);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        return await DbSet
            .Include(p => p.Brand)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Sku == normalizedSku, ct);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null, CancellationToken ct = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        var query = DbSet.Where(p => p.Sku == normalizedSku);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null, CancellationToken ct = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var query = DbSet.Where(p => p.Slug == normalizedSlug);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }
}
