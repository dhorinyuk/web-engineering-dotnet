using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public class CreateProductDto
{
    [Required(ErrorMessage = "BrandId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "BrandId must be greater than zero.")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(64, MinimumLength = 3, ErrorMessage = "SKU must be between 3 and 64 characters.")]
    public string Sku { get; set; } = null!;

    [Required(ErrorMessage = "Slug is required.")]
    [StringLength(220, MinimumLength = 2, ErrorMessage = "Slug must be between 2 and 220 characters.")]
    public string Slug { get; set; } = null!;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 200 characters.")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, 10000000.00, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Range(0.00, 10000000.00, ErrorMessage = "Cost price cannot be negative.")]
    public decimal CostPrice { get; set; }

    [Range(0.00, 10000.00, ErrorMessage = "Weight cannot be negative.")]
    public decimal WeightKg { get; set; }

    [Range(0, 1000000, ErrorMessage = "Initial stock cannot be negative.")]
    public int Stock { get; set; }

    public bool IsAvailable { get; set; } = true;

    public CreateProductDetailsDto? Details { get; set; }
    public List<CreateProductImageDto>? Images { get; set; }
    public List<int>? CategoryIds { get; set; }
    public int? PrimaryCategoryId { get; set; }
}
