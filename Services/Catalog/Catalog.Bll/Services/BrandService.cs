using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.ServiceDefaults.Caching;

namespace Catalog.Bll.Services;

public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> GetAllBrandsAsync(CancellationToken ct = default);
    Task<BrandDto> GetBrandByIdAsync(int id, CancellationToken ct = default);
    Task<BrandDto> CreateBrandAsync(CreateBrandDto dto, CancellationToken ct = default);
}

public class BrandService : IBrandService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ITwoTierCacheService? _cache;
    private readonly IMemoryCache? _memoryCache;
    private readonly ILogger<BrandService>? _logger;

    public BrandService(
        IUnitOfWork uow,
        IMapper mapper,
        ITwoTierCacheService? cache = null,
        IMemoryCache? memoryCache = null,
        ILogger<BrandService>? logger = null)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public BrandService(IUnitOfWork uow, IMapper mapper)
        : this(uow, mapper, null, null, null)
    {
    }

    public async Task<IReadOnlyList<BrandDto>> GetAllBrandsAsync(CancellationToken ct = default)
    {
        const string key = "catalog:brands";
        if (_cache != null)
        {
            var cached = await _cache.GetOrSetAsync(key, async () =>
            {
                var brands = await _uow.Brands.GetAllAsync(ct);
                return (IReadOnlyList<BrandDto>)_mapper.Map<IReadOnlyList<BrandDto>>(brands);
            }, ct: ct);

            if (cached != null) return cached;
        }

        var list = await _uow.Brands.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<BrandDto>>(list);
    }

    public async Task<BrandDto> GetBrandByIdAsync(int id, CancellationToken ct = default)
    {
        var key = $"catalog:brand:{id}";
        if (_cache != null)
        {
            var cached = await _cache.GetOrSetAsync(key, async () =>
            {
                var brand = await _uow.Brands.GetByIdAsync(id, ct);
                return brand == null ? null : _mapper.Map<BrandDto>(brand);
            }, ct: ct);

            if (cached != null) return cached;
        }

        var b = await _uow.Brands.GetByIdAsync(id, ct);
        if (b == null)
            throw new EntityNotFoundException(nameof(Brand), id);

        return _mapper.Map<BrandDto>(b);
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto dto, CancellationToken ct = default)
    {
        if (await _uow.Brands.ExistsByCodeAsync(dto.Code, null, ct))
            throw new ConflictException($"Brand with code '{dto.Code}' already exists.");

        if (await _uow.Brands.ExistsByNameAsync(dto.Name, null, ct))
            throw new ConflictException($"Brand with name '{dto.Name}' already exists.");

        var brand = new Brand(dto.Name, dto.Code, dto.Country, dto.IsActive);
        await _uow.Brands.AddAsync(brand, ct);
        await _uow.SaveChangesAsync(ct);

        // Cache invalidation
        _memoryCache?.Remove("catalog:brands");
        if (_cache != null)
        {
            await _cache.RemoveAsync("catalog:brands", "Brand created", ct);
        }
        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", "catalog:brands", "Brand created");

        return _mapper.Map<BrandDto>(brand);
    }
}
