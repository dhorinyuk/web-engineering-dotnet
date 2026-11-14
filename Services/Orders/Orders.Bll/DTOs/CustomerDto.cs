namespace Orders.Bll.DTOs;

public class CustomerDto
{
    public long Id { get; set; }
    public Guid ExternalId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
