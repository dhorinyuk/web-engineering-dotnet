namespace Catalog.Bll.Dtos;

public record ProductCategoryItemDto(
    int CategoryId,
    string CategoryName,
    string CategorySlug,
    bool IsPrimary,
    DateTime AddedAt);

public record ProductDto(
    int Id,
    int BrandId,
    string? BrandName,
    string Sku,
    string Slug,
    string Name,
    decimal Price,
    decimal CostPrice,
    decimal WeightKg,
    int Stock,
    bool IsAvailable,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    ProductDetailsDto? Details,
    List<ProductImageDto> Images,
    List<ProductCategoryItemDto> Categories);
