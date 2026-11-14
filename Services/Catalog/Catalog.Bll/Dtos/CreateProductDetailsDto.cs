using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public class CreateProductDetailsDto
{
    [Required(ErrorMessage = "Technical description is required.")]
    public string TechnicalDescription { get; set; } = null!;

    [Range(0, 240, ErrorMessage = "Warranty months must be between 0 and 240.")]
    public int WarrantyMonths { get; set; } = 12;

    [Required(ErrorMessage = "Country of origin is required.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 60 characters.")]
    public string CountryOfOrigin { get; set; } = null!;

    [StringLength(100, ErrorMessage = "Dimensions cannot exceed 100 characters.")]
    public string? Dimensions { get; set; }

    [StringLength(255, ErrorMessage = "Installation guide URL cannot exceed 255 characters.")]
    [Url(ErrorMessage = "Installation guide URL must be a valid URL.")]
    public string? InstallationGuideUrl { get; set; }
}

public class CreateProductImageDto
{
    [Required(ErrorMessage = "Image URL is required.")]
    [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters.")]
    [Url(ErrorMessage = "Image URL must be a valid URL.")]
    public string Url { get; set; } = null!;

    [Required(ErrorMessage = "Image caption is required.")]
    [StringLength(200, ErrorMessage = "Image caption cannot exceed 200 characters.")]
    public string Caption { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }
}
