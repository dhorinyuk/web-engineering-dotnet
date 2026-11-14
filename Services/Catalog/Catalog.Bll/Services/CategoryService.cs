using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.ServiceDefaults.Caching;

namespace Catalog.Bll.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDto> GetCategoryByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken ct = default);
}

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ITwoTierCacheService? _cache;
    private readonly IMemoryCache? _memoryCache;
    private readonly ILogger<CategoryService>? _logger;

    public CategoryService(
        IUnitOfWork uow,
        IMapper mapper,
        ITwoTierCacheService? cache = null,
        IMemoryCache? memoryCache = null,
        ILogger<CategoryService>? logger = null)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public CategoryService(IUnitOfWork uow, IMapper mapper)
        : this(uow, mapper, null, null, null)
    {
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken ct = default)
    {
        const string key = "catalog:categories";
        if (_cache != null)
        {
            var cached = await _cache.GetOrSetAsync(key, async () =>
            {
                var categories = await _uow.Categories.GetAllAsync(ct);
                return (IReadOnlyList<CategoryDto>)_mapper.Map<IReadOnlyList<CategoryDto>>(categories);
            }, ct: ct);

            if (cached != null) return cached;
        }

        var list = await _uow.Categories.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<CategoryDto>>(list);
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        var key = $"catalog:category:{id}";
        if (_cache != null)
        {
            var cached = await _cache.GetOrSetAsync(key, async () =>
            {
                var category = await _uow.Categories.GetWithSubCategoriesAsync(id, ct);
                return category == null ? null : _mapper.Map<CategoryDto>(category);
            }, ct: ct);

            if (cached != null) return cached;
        }

        var cat = await _uow.Categories.GetWithSubCategoriesAsync(id, ct);
        if (cat == null)
            throw new EntityNotFoundException(nameof(Category), id);

        return _mapper.Map<CategoryDto>(cat);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        if (await _uow.Categories.ExistsBySlugAsync(dto.Slug, null, ct))
            throw new ConflictException($"Category with slug '{dto.Slug}' already exists.");

        if (dto.ParentCategoryId.HasValue)
        {
            var parent = await _uow.Categories.GetByIdAsync(dto.ParentCategoryId.Value, ct);
            if (parent == null)
                throw new EntityNotFoundException(nameof(Category), dto.ParentCategoryId.Value);
        }

        var category = new Category(dto.Name, dto.Slug, dto.ParentCategoryId, dto.DisplayOrder, dto.IsActive);
        await _uow.Categories.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);

        // Cache invalidation
        _memoryCache?.Remove("catalog:categories");
        if (_cache != null)
        {
            await _cache.RemoveAsync("catalog:categories", "Category created", ct);
        }
        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", "catalog:categories", "Category created");

        return _mapper.Map<CategoryDto>(category);
    }
}
