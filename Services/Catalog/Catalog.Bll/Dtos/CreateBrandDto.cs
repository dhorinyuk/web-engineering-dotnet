using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public class CreateBrandDto
{
    [Required(ErrorMessage = "Brand name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Brand name must be between 2 and 100 characters.")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Brand code is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Brand code must be between 2 and 50 characters.")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Country is required.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 60 characters.")]
    public string Country { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}

public class CreateCategoryDto
{
    public int? ParentCategoryId { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters.")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Slug is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Slug must be between 2 and 120 characters.")]
    public string Slug { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
