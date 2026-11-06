using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

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

    public BrandService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<BrandDto>> GetAllBrandsAsync(CancellationToken ct = default)
    {
        var brands = await _uow.Brands.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<BrandDto>>(brands);
    }

    public async Task<BrandDto> GetBrandByIdAsync(int id, CancellationToken ct = default)
    {
        var brand = await _uow.Brands.GetByIdAsync(id, ct);
        if (brand == null)
            throw new EntityNotFoundException(nameof(Brand), id);

        return _mapper.Map<BrandDto>(brand);
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

        return _mapper.Map<BrandDto>(brand);
    }
}
