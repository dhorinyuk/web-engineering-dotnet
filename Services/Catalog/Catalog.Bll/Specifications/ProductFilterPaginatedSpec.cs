using Catalog.Bll.Common;
using Catalog.Dal.Specifications;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public class ProductFilterPaginatedSpec : BaseSpecification<Product>
{
    public ProductFilterPaginatedSpec(ProductQueryParams query)
        : base(p =>
            (string.IsNullOrWhiteSpace(query.SearchTerm) ||
             p.Name.Contains(query.SearchTerm) ||
             p.Sku.Contains(query.SearchTerm) ||
             p.Slug.Contains(query.SearchTerm)) &&
            (!query.BrandId.HasValue || p.BrandId == query.BrandId.Value) &&
            (!query.CategoryId.HasValue || p.ProductCategories.Any(pc => pc.CategoryId == query.CategoryId.Value)) &&
            (!query.MinPrice.HasValue || p.Price >= query.MinPrice.Value) &&
            (!query.MaxPrice.HasValue || p.Price <= query.MaxPrice.Value) &&
            (!query.IsAvailable.HasValue || p.IsAvailable == query.IsAvailable.Value))
    {
        // Includes for eager loading needed relations (Criterion 14)
        AddInclude(p => p.Brand!);
        AddInclude(p => p.Details!);
        AddInclude(p => p.Images);
        AddInclude("ProductCategories.Category");

        // Dynamic sorting encapsulated in specification
        var sortBy = query.SortBy?.ToLowerInvariant();
        if (query.SortDescending)
        {
            switch (sortBy)
            {
                case "price":
                    ApplyOrderByDescending(p => p.Price);
                    break;
                case "name":
                    ApplyOrderByDescending(p => p.Name);
                    break;
                case "createdat":
                default:
                    ApplyOrderByDescending(p => p.CreatedAt);
                    break;
            }
        }
        else
        {
            switch (sortBy)
            {
                case "price":
                    ApplyOrderBy(p => p.Price);
                    break;
                case "name":
                    ApplyOrderBy(p => p.Name);
                    break;
                case "createdat":
                    ApplyOrderBy(p => p.CreatedAt);
                    break;
                default:
                    ApplyOrderBy(p => p.Id);
                    break;
            }
        }

        // Apply paging via Skip and Take (Criterion 10 & 14)
        var skip = (query.Page - 1) * query.PageSize;
        ApplyPaging(skip, query.PageSize);
    }
}
