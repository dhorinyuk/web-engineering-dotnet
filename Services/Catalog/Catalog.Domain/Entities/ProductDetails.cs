using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class ProductDetails
{
    public int ProductId { get; private set; }
    public string TechnicalDescription { get; private set; } = null!;
    public int WarrantyMonths { get; private set; }
    public string CountryOfOrigin { get; private set; } = null!;
    public string? Dimensions { get; private set; }
    public string? InstallationGuideUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Product Product { get; private set; } = null!;

    private ProductDetails()
    {
    }

    public ProductDetails(
        int productId,
        string technicalDescription,
        int warrantyMonths,
        string countryOfOrigin,
        string? dimensions = null,
        string? installationGuideUrl = null)
    {
        ValidateInvariants(technicalDescription, warrantyMonths, countryOfOrigin);

        ProductId = productId;
        TechnicalDescription = technicalDescription.Trim();
        WarrantyMonths = warrantyMonths;
        CountryOfOrigin = countryOfOrigin.Trim();
        Dimensions = dimensions?.Trim();
        InstallationGuideUrl = installationGuideUrl?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateTechnicalSpecs(
        string technicalDescription,
        int warrantyMonths,
        string countryOfOrigin,
        string? dimensions,
        string? installationGuideUrl)
    {
        ValidateInvariants(technicalDescription, warrantyMonths, countryOfOrigin);

        TechnicalDescription = technicalDescription.Trim();
        WarrantyMonths = warrantyMonths;
        CountryOfOrigin = countryOfOrigin.Trim();
        Dimensions = dimensions?.Trim();
        InstallationGuideUrl = installationGuideUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateInvariants(string technicalDescription, int warrantyMonths, string countryOfOrigin)
    {
        if (string.IsNullOrWhiteSpace(technicalDescription))
            throw new DomainException("Technical description cannot be empty.");

        if (warrantyMonths < 0)
            throw new DomainException("Warranty months cannot be negative.");

        if (string.IsNullOrWhiteSpace(countryOfOrigin))
            throw new DomainException("Country of origin cannot be empty.");

        if (countryOfOrigin.Length is < 2 or > 60)
            throw new DomainException("Country of origin must be between 2 and 60 characters.");
    }
}
