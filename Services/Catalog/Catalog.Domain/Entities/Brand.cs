using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Brand
{
    private readonly List<Product> _products = new();

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    // Private constructor required by EF Core
    private Brand()
    {
    }

    public Brand(string name, string code, string country, bool isActive = true)
    {
        ValidateInvariants(name, code, country);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Country = country.Trim();
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string country, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Brand name cannot be empty.");

        if (name.Length is < 2 or > 100)
            throw new DomainException("Brand name must be between 2 and 100 characters.");

        if (string.IsNullOrWhiteSpace(country))
            throw new DomainException("Brand country cannot be empty.");

        Name = name.Trim();
        Country = country.Trim();
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateInvariants(string name, string code, string country)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Brand name cannot be empty.");

        if (name.Length is < 2 or > 100)
            throw new DomainException("Brand name must be between 2 and 100 characters.");

        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Brand code cannot be empty.");

        if (code.Length is < 2 or > 50)
            throw new DomainException("Brand code must be between 2 and 50 characters.");

        if (string.IsNullOrWhiteSpace(country))
            throw new DomainException("Brand country cannot be empty.");

        if (country.Length is < 2 or > 60)
            throw new DomainException("Brand country must be between 2 and 60 characters.");
    }
}
