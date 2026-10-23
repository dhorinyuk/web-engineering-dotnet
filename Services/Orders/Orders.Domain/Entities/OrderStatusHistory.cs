namespace Orders.Domain.Entities;

public class OrderStatusHistory
{
    public long HistoryId { get; set; }
    public long OrderId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
}
