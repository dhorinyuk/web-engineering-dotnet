using AutoMapper;
using Catalog.Bll.Common;
using Catalog.Bll.Dtos;
using Catalog.Bll.Specifications;
using Catalog.Dal;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Bll.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    // Criterion 10 & 14: Pagination, filtering, and sorting executed via Specification at SQL level
    public async Task<PagedResult<ProductDto>> GetProductsPagedAsync(ProductQueryParams query, CancellationToken ct = default)
    {
        var filterSpec = new ProductFilterPaginatedSpec(query);
        var countSpec = new ProductFilterCountSpec(query);

        var totalCount = await _uow.Products.CountAsync(countSpec, ct);
        var products = await _uow.Products.ListAsync(filterSpec, ct);

        var dtos = _mapper.Map<IReadOnlyList<ProductDto>>(products);
        return new PagedResult<ProductDto>(dtos, totalCount, query.Page, query.PageSize);
    }

    // Criterion 6: Specific Repo 1 - Eager Loading with full graph
    public async Task<ProductDto> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetWithAllDetailsAsync(id, ct);
        if (product == null)
            throw new EntityNotFoundException(nameof(Product), id);

        return _mapper.Map<ProductDto>(product);
    }

    // Criterion 6: Specific Repo 2 - Explicit Loading on-demand
    public async Task<ProductDto> GetProductWithExplicitImagesAsync(int id, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetWithExplicitImagesAsync(id, ct);
        if (product == null)
            throw new EntityNotFoundException(nameof(Product), id);

        return _mapper.Map<ProductDto>(product);
    }

    // Criterion 6: Specific Repo 3 - LINQ to Entities M:N filtered by AddedAt
    public async Task<IReadOnlyList<ProductDto>> GetRecentCategoryProductsAsync(int categoryId, int days, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetByIdAsync(categoryId, ct);
        if (category == null)
            throw new EntityNotFoundException(nameof(Category), categoryId);

        var since = DateTime.UtcNow.AddDays(-Math.Max(1, days));
        var products = await _uow.Products.GetProductsAssignedToCategorySinceAsync(categoryId, since, ct);

        return _mapper.Map<IReadOnlyList<ProductDto>>(products);
    }

    // Criterion 7: Creation with DB-level invariant checks, domain encapsulation and AutoMapper
    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        // 1. Validate Brand exists
        var brand = await _uow.Brands.GetByIdAsync(dto.BrandId, ct);
        if (brand == null)
            throw new EntityNotFoundException(nameof(Brand), dto.BrandId);

        // 2. Validate DB-level uniqueness rules (SKU and Slug)
        if (await _uow.Products.ExistsBySkuAsync(dto.Sku, null, ct))
            throw new ConflictException($"Product with SKU '{dto.Sku}' already exists.");

        if (await _uow.Products.ExistsBySlugAsync(dto.Slug, null, ct))
            throw new ConflictException($"Product with Slug '{dto.Slug}' already exists.");

        // 3. Create aggregate root via domain constructor
        var product = new Product(
            brandId: dto.BrandId,
            sku: dto.Sku,
            slug: dto.Slug,
            name: dto.Name,
            price: dto.Price,
            costPrice: dto.CostPrice,
            weightKg: dto.WeightKg,
            stock: dto.Stock,
            isAvailable: dto.IsAvailable);

        // 4. Attach 1:1 details if provided
        if (dto.Details != null)
        {
            var details = new ProductDetails(
                productId: 0,
                technicalDescription: dto.Details.TechnicalDescription,
                warrantyMonths: dto.Details.WarrantyMonths,
                countryOfOrigin: dto.Details.CountryOfOrigin,
                dimensions: dto.Details.Dimensions,
                installationGuideUrl: dto.Details.InstallationGuideUrl);

            product.SetProductDetails(details);
        }

        // 5. Attach 1:N images if provided
        if (dto.Images != null)
        {
            foreach (var img in dto.Images)
            {
                product.AddImage(img.Url, img.Caption, img.DisplayOrder, img.IsPrimary);
            }
        }

        // 6. Attach M:N category assignments
        if (dto.CategoryIds != null)
        {
            foreach (var catId in dto.CategoryIds)
            {
                var isPrimary = dto.PrimaryCategoryId.HasValue && dto.PrimaryCategoryId.Value == catId;
                product.AssignCategory(catId, isPrimary);
            }
        }

        await _uow.Products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        // Reload product with full graph for mapping
        var created = await _uow.Products.GetWithAllDetailsAsync(product.Id, ct) ?? product;
        return _mapper.Map<ProductDto>(created);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetWithAllDetailsAsync(id, ct);
        if (product == null)
            throw new EntityNotFoundException(nameof(Product), id);

        if (dto.BrandId.HasValue && dto.BrandId.Value != product.BrandId)
        {
            var brand = await _uow.Brands.GetByIdAsync(dto.BrandId.Value, ct);
            if (brand == null)
                throw new EntityNotFoundException(nameof(Brand), dto.BrandId.Value);
        }

        if (await _uow.Products.ExistsBySlugAsync(dto.Slug, id, ct))
            throw new ConflictException($"Product with Slug '{dto.Slug}' already exists.");

        // Domain method mutation
        product.UpdateDetails(dto.Name, dto.Slug, dto.WeightKg, dto.BrandId);

        if (dto.IsAvailable.HasValue)
            product.SetAvailability(dto.IsAvailable.Value);

        if (dto.Details != null)
        {
            if (product.Details != null)
            {
                product.Details.UpdateTechnicalSpecs(
                    dto.Details.TechnicalDescription,
                    dto.Details.WarrantyMonths,
                    dto.Details.CountryOfOrigin,
                    dto.Details.Dimensions,
                    dto.Details.InstallationGuideUrl);
            }
            else
            {
                var newDetails = new ProductDetails(
                    productId: product.Id,
                    technicalDescription: dto.Details.TechnicalDescription,
                    warrantyMonths: dto.Details.WarrantyMonths,
                    countryOfOrigin: dto.Details.CountryOfOrigin,
                    dimensions: dto.Details.Dimensions,
                    installationGuideUrl: dto.Details.InstallationGuideUrl);
                product.SetProductDetails(newDetails);
            }
        }

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(ct);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdatePriceAsync(int id, UpdatePriceDto dto, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetWithAllDetailsAsync(id, ct);
        if (product == null)
            throw new EntityNotFoundException(nameof(Product), id);

        // Domain method mutation (Criterion 1)
        product.UpdatePrice(dto.Price, dto.CostPrice);

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(ct);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateStockAsync(int id, UpdateStockDto dto, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetWithAllDetailsAsync(id, ct);
        if (product == null)
            throw new EntityNotFoundException(nameof(Product), id);

        // Domain method mutations with invariant validation
        if (dto.Operation.Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            product.AddStock(dto.Quantity);
        }
        else
        {
            product.RemoveStock(dto.Quantity);
        }

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(ct);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteProductAsync(int id, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct);
        if (product == null)
            throw new EntityNotFoundException(nameof(Product), id);

        _uow.Products.Delete(product);
        await _uow.SaveChangesAsync(ct);
    }
}
