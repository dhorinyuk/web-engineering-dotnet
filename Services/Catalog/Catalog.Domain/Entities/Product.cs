using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Product
{
    private readonly List<ProductImage> _images = new();
    private readonly List<ProductCategory> _productCategories = new();

    public int Id { get; private set; }
    public int BrandId { get; private set; }
    public string Sku { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal WeightKg { get; private set; }
    public int Stock { get; private set; }
    public bool IsAvailable { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Brand? Brand { get; private set; }
    public ProductDetails? Details { get; private set; }
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories.AsReadOnly();

    // Private constructor for EF Core
    private Product()
    {
    }

    public Product(
        int brandId,
        string sku,
        string slug,
        string name,
        decimal price,
        decimal costPrice,
        decimal weightKg,
        int stock = 0,
        bool isAvailable = true)
    {
        ValidateInvariants(sku, slug, name, price, costPrice, weightKg, stock);

        if (brandId <= 0)
            throw new DomainException("Brand identifier must be greater than zero.");

        BrandId = brandId;
        Sku = sku.Trim().ToUpperInvariant();
        Slug = slug.Trim().ToLowerInvariant();
        Name = name.Trim();
        Price = price;
        CostPrice = costPrice;
        WeightKg = weightKg;
        Stock = stock;
        IsAvailable = isAvailable;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newPrice, decimal? newCostPrice = null)
    {
        if (newPrice <= 0)
            throw new DomainException($"Product price must be greater than zero. Provided: {newPrice}");

        if (newCostPrice.HasValue && newCostPrice.Value < 0)
            throw new DomainException($"Product cost price cannot be negative. Provided: {newCostPrice.Value}");

        Price = newPrice;
        if (newCostPrice.HasValue)
            CostPrice = newCostPrice.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException($"Stock quantity to add must be positive. Provided: {quantity}");

        Stock += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException($"Stock quantity to remove must be positive. Provided: {quantity}");

        if (Stock < quantity)
            throw new DomainException($"Insufficient stock for product '{Sku}'. Current stock: {Stock}, requested: {quantity}");

        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string slug, decimal weightKg, int? brandId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");

        if (name.Length is < 3 or > 200)
            throw new DomainException("Product name length must be between 3 and 200 characters.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Product slug cannot be empty.");

        if (weightKg < 0)
            throw new DomainException("Product weight cannot be negative.");

        if (brandId.HasValue)
        {
            if (brandId.Value <= 0)
                throw new DomainException("Brand ID must be greater than zero.");
            BrandId = brandId.Value;
        }

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        WeightKg = weightKg;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetProductDetails(ProductDetails details)
    {
        Details = details ?? throw new DomainException("Product details cannot be null.");
        UpdatedAt = DateTime.UtcNow;
    }

    public ProductImage AddImage(string url, string caption, int displayOrder = 0, bool isPrimary = false)
    {
        if (isPrimary)
        {
            foreach (var img in _images)
            {
                img.SetPrimary(false);
            }
        }

        var image = new ProductImage(Id, url, caption, displayOrder, isPrimary);
        _images.Add(image);
        UpdatedAt = DateTime.UtcNow;
        return image;
    }

    public void RemoveImage(int imageId)
    {
        var existing = _images.FirstOrDefault(i => i.Id == imageId);
        if (existing == null)
            throw new DomainException($"Image with ID '{imageId}' does not exist on product '{Sku}'.");

        _images.Remove(existing);
        UpdatedAt = DateTime.UtcNow;
    }

    public ProductCategory AssignCategory(int categoryId, bool isPrimary = false)
    {
        if (categoryId <= 0)
            throw new DomainException("Category ID must be greater than zero.");

        var existing = _productCategories.FirstOrDefault(c => c.CategoryId == categoryId);
        if (existing != null)
        {
            if (isPrimary && !existing.IsPrimary)
                existing.SetPrimary(true);
            return existing;
        }

        if (isPrimary)
        {
            foreach (var cat in _productCategories)
            {
                cat.SetPrimary(false);
            }
        }

        var productCategory = new ProductCategory(Id, categoryId, isPrimary);
        _productCategories.Add(productCategory);
        UpdatedAt = DateTime.UtcNow;
        return productCategory;
    }

    public void RemoveCategory(int categoryId)
    {
        var existing = _productCategories.FirstOrDefault(c => c.CategoryId == categoryId);
        if (existing != null)
        {
            _productCategories.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    private static void ValidateInvariants(
        string sku,
        string slug,
        string name,
        decimal price,
        decimal costPrice,
        decimal weightKg,
        int stock)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Product SKU cannot be empty.");

        if (sku.Length is < 3 or > 64)
            throw new DomainException("Product SKU length must be between 3 and 64 characters.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Product slug cannot be empty.");

        if (slug.Length is < 2 or > 220)
            throw new DomainException("Product slug length must be between 2 and 220 characters.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");

        if (name.Length is < 3 or > 200)
            throw new DomainException("Product name length must be between 3 and 200 characters.");

        if (price <= 0)
            throw new DomainException($"Product price must be greater than zero. Provided: {price}");

        if (costPrice < 0)
            throw new DomainException($"Product cost price cannot be negative. Provided: {costPrice}");

        if (weightKg < 0)
            throw new DomainException($"Product weight cannot be negative. Provided: {weightKg}");

        if (stock < 0)
            throw new DomainException($"Product initial stock cannot be negative. Provided: {stock}");
    }
}
