using System.ComponentModel.DataAnnotations;

namespace Orders.Bll.DTOs;

public class CreateOrderItemDto
{
    [Range(1, long.MaxValue, ErrorMessage = "ProductId must be positive.")]
    public long ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "Units must be between 1 and 1000.")]
    public int Units { get; set; }
}
