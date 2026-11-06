using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

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

    public CategoryService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _uow.Categories.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<CategoryDto>>(categories);
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetWithSubCategoriesAsync(id, ct);
        if (category == null)
            throw new EntityNotFoundException(nameof(Category), id);

        return _mapper.Map<CategoryDto>(category);
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

        return _mapper.Map<CategoryDto>(category);
    }
}
