namespace Catalog.Bll.Dtos;

public record BrandDto(
    int Id,
    string Name,
    string Code,
    string Country,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CategoryDto(
    int Id,
    int? ParentCategoryId,
    string Name,
    string Slug,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<CategoryDto>? SubCategories = null);
