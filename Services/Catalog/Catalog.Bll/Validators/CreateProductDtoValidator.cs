using Catalog.Bll.Dtos;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.BrandId)
            .GreaterThan(0)
            .WithMessage("BrandId must be greater than zero.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .Length(3, 64).WithMessage("SKU must be between 3 and 64 characters.")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("SKU can only contain alphanumeric characters, hyphens, and underscores.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .Length(2, 220).WithMessage("Slug must be between 2 and 220 characters.")
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .Length(3, 200).WithMessage("Product name must be between 3 and 200 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0.00m).WithMessage("Price must be strictly positive (> 0.00).");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0.00m).WithMessage("Cost price cannot be negative.");

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0.000m).WithMessage("Weight cannot be negative.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Initial stock cannot be negative.");

        When(x => x.Details != null, () =>
        {
            RuleFor(x => x.Details!.TechnicalDescription)
                .NotEmpty().WithMessage("Technical description cannot be empty.");

            RuleFor(x => x.Details!.WarrantyMonths)
                .GreaterThanOrEqualTo(0).WithMessage("Warranty months cannot be negative.");

            RuleFor(x => x.Details!.CountryOfOrigin)
                .NotEmpty().WithMessage("Country of origin is required.")
                .Length(2, 60).WithMessage("Country of origin must be between 2 and 60 characters.");
        });

        RuleForEach(x => x.Images).ChildRules(image =>
        {
            image.RuleFor(i => i.Url)
                .NotEmpty().WithMessage("Image URL is required.")
                .MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters.");

            image.RuleFor(i => i.Caption)
                .NotEmpty().WithMessage("Image caption is required.")
                .MaximumLength(200).WithMessage("Image caption cannot exceed 200 characters.");
        });
    }
}
