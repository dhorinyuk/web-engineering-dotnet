namespace Catalog.Domain.Entities;

public class ProductCategory
{
    public int ProductId { get; private set; }
    public int CategoryId { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime AddedAt { get; private set; }

    public Product Product { get; private set; } = null!;
    public Category Category { get; private set; } = null!;

    private ProductCategory()
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
