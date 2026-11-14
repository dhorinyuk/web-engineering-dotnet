using Catalog.Bll.Common;
using Catalog.Dal.Specifications;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public class ProductFilterCountSpec : BaseSpecification<Product>
{
    public ProductFilterCountSpec(ProductQueryParams query)
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
    }
}
