using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class ProductImage
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string Url { get; private set; } = null!;
    public string Caption { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Product Product { get; private set; } = null!;

    private ProductImage()
    {
    }

    public ProductImage(int productId, string url, string caption, int displayOrder = 0, bool isPrimary = false)
    {
        ValidateInvariants(url, caption);

        ProductId = productId;
        Url = url.Trim();
        Caption = caption.Trim();
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string url, string caption, int displayOrder, bool isPrimary)
    {
        ValidateInvariants(url, caption);

        Url = url.Trim();
        Caption = caption.Trim();
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    private static void ValidateInvariants(string url, string caption)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Image URL cannot be empty.");

        if (url.Length > 500)
            throw new DomainException("Image URL cannot exceed 500 characters.");

        if (string.IsNullOrWhiteSpace(caption))
            throw new DomainException("Image caption cannot be empty.");

        if (caption.Length > 200)
            throw new DomainException("Image caption cannot exceed 200 characters.");
    }
}
