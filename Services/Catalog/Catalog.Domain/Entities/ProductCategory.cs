namespace Catalog.Domain.Entities;

public class ProductCategory
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public DateTime AddedAt { get; set; }
    public bool IsPrimary { get; set; }

    public ProductCategory()
    {
    }

    public ProductCategory(int productId, int categoryId, bool isPrimary = false, DateTime? addedAt = null)
    {
        ProductId = productId;
        CategoryId = categoryId;
        IsPrimary = isPrimary;
        AddedAt = addedAt ?? DateTime.UtcNow;
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }
}
