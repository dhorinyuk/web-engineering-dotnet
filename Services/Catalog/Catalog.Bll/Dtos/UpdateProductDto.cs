using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public class UpdateProductDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 200 characters.")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Slug is required.")]
    [StringLength(220, MinimumLength = 2, ErrorMessage = "Slug must be between 2 and 220 characters.")]
    public string Slug { get; set; } = null!;

    [Range(0.00, 10000.00, ErrorMessage = "Weight cannot be negative.")]
    public decimal WeightKg { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BrandId must be greater than zero.")]
    public int? BrandId { get; set; }

    public bool? IsAvailable { get; set; }

    public CreateProductDetailsDto? Details { get; set; }
}

public class UpdatePriceDto
{
    [Required(ErrorMessage = "New price is required.")]
    [Range(0.01, 10000000.00, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Range(0.00, 10000000.00, ErrorMessage = "Cost price cannot be negative.")]
    public decimal? CostPrice { get; set; }
}

public class UpdateStockDto
{
    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, 100000, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Operation type is required ('add' or 'remove').")]
    [RegularExpression("^(add|remove)$", ErrorMessage = "Operation must be either 'add' or 'remove'.")]
    public string Operation { get; set; } = "add";
}
