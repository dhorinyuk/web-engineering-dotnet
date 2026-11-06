using Catalog.Bll.Common;
using Catalog.Bll.Dtos;

namespace Catalog.Bll.Services;

public interface IProductService
{
    // Criterion 10 & 14: Pagination, filtering, sorting via Specification at SQL level
    Task<PagedResult<ProductDto>> GetProductsPagedAsync(ProductQueryParams query, CancellationToken ct = default);

    // Criterion 6: Specific Repo 1 - Eager loading
    Task<ProductDto> GetProductByIdAsync(int id, CancellationToken ct = default);

    // Criterion 6: Specific Repo 2 - Explicit loading
    Task<ProductDto> GetProductWithExplicitImagesAsync(int id, CancellationToken ct = default);

    // Criterion 6: Specific Repo 3 - LINQ to Entities M:N query via junction entity
    Task<IReadOnlyList<ProductDto>> GetRecentCategoryProductsAsync(int categoryId, int days, CancellationToken ct = default);

    // Criterion 7 & 8: CRUD with domain methods, UoW, DTO, AutoMapper, Conflict/NotFound exceptions
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default);
    Task<ProductDto> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken ct = default);
    Task<ProductDto> UpdatePriceAsync(int id, UpdatePriceDto dto, CancellationToken ct = default);
    Task<ProductDto> UpdateStockAsync(int id, UpdateStockDto dto, CancellationToken ct = default);
    Task DeleteProductAsync(int id, CancellationToken ct = default);
}
