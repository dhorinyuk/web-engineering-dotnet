namespace Orders.Domain.Entities;

public class Order
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string Status { get; set; } = "Submitted";
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "SYSTEM";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public OrderDetails? Details { get; set; }
}
