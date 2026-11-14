using System.Diagnostics;
using System.Text.Json;
using AutoMapper;
using Catalog.Bll.Common;
using Catalog.Bll.Dtos;
using Catalog.Bll.Specifications;
using Catalog.Dal;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.ServiceDefaults.Caching;

namespace Catalog.Bll.Services;

public class ProductService : IProductService
{
    private static readonly ActivitySource ActivitySource = new("Catalog.Api");
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IMemoryCache? _cache;
    private readonly IDistributedCache? _distributedCache;
    private readonly CacheMetrics? _cacheMetrics;
    private readonly ILogger<ProductService>? _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProductService(
        IUnitOfWork uow,
        IMapper mapper,
        IMemoryCache? cache = null,
        IDistributedCache? distributedCache = null,
        CacheMetrics? cacheMetrics = null,
        ILogger<ProductService>? logger = null)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _cache = cache;
        _distributedCache = distributedCache;
        _cacheMetrics = cacheMetrics;
        _logger = logger;
    }

    public ProductService(IUnitOfWork uow, IMapper mapper, ILogger<ProductService>? logger)
        : this(uow, mapper, null, null, null, logger)
    {
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

    // Criterion 6: Specific Repo 1 - Eager Loading with full graph & Two-Tier Cache-aside (L1 Memory + L2 Redis)
    public async Task<ProductDto> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = $"catalog:product:{id}";

        // 1. L1 Memory Cache Check
        if (_cache != null && _cache.TryGetValue(cacheKey, out ProductDto? cached) && cached != null)
        {
            _cacheMetrics?.RecordHit("L1");
            _logger?.LogDebug("Cache L1 HIT for key {Key}", cacheKey);
            return cached;
        }

        _cacheMetrics?.RecordMiss("L1");
        _logger?.LogDebug("Cache L1 MISS for key {Key}", cacheKey);

        // 2. L2 Redis Distributed Cache Check
        if (_distributedCache != null)
        {
            try
            {
                var l2Json = await _distributedCache.GetStringAsync(cacheKey, ct);
                if (!string.IsNullOrEmpty(l2Json))
                {
                    _cacheMetrics?.RecordHit("L2");
                    _logger?.LogDebug("Cache L2 HIT for key {Key}", cacheKey);
                    var deserialized = JsonSerializer.Deserialize<ProductDto>(l2Json, JsonOptions);
                    if (deserialized != null)
                    {
                        var l1Options = new MemoryCacheEntryOptions
                        {
                            Size = 1,
                            SlidingExpiration = TimeSpan.FromMinutes(2),
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        };
                        _cache?.Set(cacheKey, deserialized, l1Options);
                        return deserialized;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to read from L2 cache for key {Key}: {Message}", cacheKey, ex.Message);
            }
        }

        _cacheMetrics?.RecordMiss("L2");
        _logger?.LogDebug("Cache L2 MISS for key {Key}", cacheKey);

        // 3. Database query (Criterion 6: Specific Repo 1 - Eager Loading with full graph)
        var product = await _uow.Products.GetWithAllDetailsAsync(id, ct);
        if (product == null)
            throw new EntityNotFoundException(nameof(Product), id);

        var dto = _mapper.Map<ProductDto>(product);

        // 4. Populate L2 and L1 on return path
        if (_distributedCache != null)
        {
            try
            {
                var l2Options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };
                var serialized = JsonSerializer.Serialize(dto, JsonOptions);
                await _distributedCache.SetStringAsync(cacheKey, serialized, l2Options, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write to L2 cache for key {Key}: {Message}", cacheKey, ex.Message);
            }
        }

        if (_cache != null)
        {
            var memOptions = new MemoryCacheEntryOptions
            {
                Size = 1,
                SlidingExpiration = TimeSpan.FromMinutes(2),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            _cache.Set(cacheKey, dto, memOptions);
        }

        return dto;
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
        using var activity = ActivitySource.StartActivity("CreateProductBusinessLogic");
        activity?.SetTag("product.sku", dto.Sku);
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
        _logger?.LogInformation("Product {ProductId} created with SKU {Sku} and price {Price}", product.Id, dto.Sku, dto.Price);
        activity?.SetTag("product.id", product.Id);

        // Cache invalidation
        _cache?.Remove("catalog:products:count");
        _distributedCache?.Remove("catalog:products:count");
        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", "catalog:products:count", "New product created");

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

        // Cache invalidation (synchronous)
        _cache?.Remove($"catalog:product:{id}");
        _distributedCache?.Remove($"catalog:product:{id}");
        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", $"catalog:product:{id}", "Product details updated");

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdatePriceAsync(int id, UpdatePriceDto dto, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("UpdateProductPriceBusinessLogic");
        activity?.SetTag("product.id", id);
        activity?.SetTag("product.price", dto.Price);

        var product = await _uow.Products.GetWithAllDetailsAsync(id, ct);
        if (product == null)
            throw new EntityNotFoundException(nameof(Product), id);

        // Domain method mutation (Criterion 1)
        product.UpdatePrice(dto.Price, dto.CostPrice);

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(ct);
        _logger?.LogInformation("Product {ProductId} price updated to {Price}", id, dto.Price);

        // Cache invalidation (synchronous)
        _cache?.Remove($"catalog:product:{id}");
        _distributedCache?.Remove($"catalog:product:{id}");
        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", $"catalog:product:{id}", "Product price updated");

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateStockAsync(int id, UpdateStockDto dto, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("UpdateProductStockBusinessLogic");
        activity?.SetTag("product.id", id);
        activity?.SetTag("operation", dto.Operation);
        activity?.SetTag("quantity", dto.Quantity);

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
        _logger?.LogInformation("Product {ProductId} stock updated: operation {Operation}, quantity {Quantity}", id, dto.Operation, dto.Quantity);

        // Cache invalidation (synchronous)
        _cache?.Remove($"catalog:product:{id}");
        _distributedCache?.Remove($"catalog:product:{id}");
        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", $"catalog:product:{id}", "Product stock updated");

        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteProductAsync(int id, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct);
        if (product == null)
            throw new EntityNotFoundException(nameof(Product), id);

        _uow.Products.Delete(product);
        await _uow.SaveChangesAsync(ct);

        // Cache invalidation (synchronous)
        _cache?.Remove($"catalog:product:{id}");
        _distributedCache?.Remove($"catalog:product:{id}");
        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", $"catalog:product:{id}", "Product deleted");
    }
}
