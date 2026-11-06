using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Category
{
    private readonly List<Category> _subCategories = new();
    private readonly List<ProductCategory> _productCategories = new();

    public int Id { get; private set; }
    public int? ParentCategoryId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Category? ParentCategory { get; private set; }
    public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();
    public IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories.AsReadOnly();

    private Category()
    {
    }

    public Category(string name, string slug, int? parentCategoryId = null, int displayOrder = 0, bool isActive = true)
    {
        ValidateInvariants(name, slug);

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        ParentCategoryId = parentCategoryId;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string slug, int displayOrder, bool isActive)
    {
        ValidateInvariants(name, slug);

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetParentCategory(int? parentCategoryId)
    {
        if (parentCategoryId.HasValue && parentCategoryId.Value == Id && Id > 0)
            throw new DomainException("Category cannot be its own parent.");

        ParentCategoryId = parentCategoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateInvariants(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name cannot be empty.");

        if (name.Length is < 2 or > 100)
            throw new DomainException("Category name must be between 2 and 100 characters.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Category slug cannot be empty.");

        if (slug.Length is < 2 or > 120)
            throw new DomainException("Category slug must be between 2 and 120 characters.");
    }
}
