using System.ComponentModel.DataAnnotations;

namespace Orders.Bll.DTOs;

public class CreateOrderDto
{
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "CustomerId must be positive.")]
    public long CustomerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<CreateOrderItemDto> Items { get; set; } = new();

    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
}
