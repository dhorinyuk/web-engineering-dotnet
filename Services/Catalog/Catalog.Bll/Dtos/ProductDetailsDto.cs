namespace Catalog.Bll.Dtos;

public record ProductImageDto(
    int Id,
    int ProductId,
    string Url,
    string Caption,
    int DisplayOrder,
    bool IsPrimary,
    DateTime CreatedAt);

public record ProductDetailsDto(
    int ProductId,
    string TechnicalDescription,
    int WarrantyMonths,
    string CountryOfOrigin,
    string? Dimensions,
    string? InstallationGuideUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
