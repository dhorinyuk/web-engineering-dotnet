using Catalog.Bll.Dtos;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .Length(3, 200).WithMessage("Product name must be between 3 and 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .Length(2, 220).WithMessage("Slug must be between 2 and 220 characters.")
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0.000m).WithMessage("Weight cannot be negative.");

        When(x => x.BrandId.HasValue, () =>
        {
            RuleFor(x => x.BrandId!.Value)
                .GreaterThan(0).WithMessage("BrandId must be greater than zero.");
        });
    }
}

public class UpdatePriceDtoValidator : AbstractValidator<UpdatePriceDto>
{
    public UpdatePriceDtoValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0.00m).WithMessage("Price must be strictly positive (> 0.00).");

        When(x => x.CostPrice.HasValue, () =>
        {
            RuleFor(x => x.CostPrice!.Value)
                .GreaterThanOrEqualTo(0.00m).WithMessage("Cost price cannot be negative.");
        });
    }
}

public class UpdateStockDtoValidator : AbstractValidator<UpdateStockDto>
{
    public UpdateStockDtoValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Stock quantity must be greater than zero.");

        RuleFor(x => x.Operation)
            .NotEmpty().WithMessage("Operation is required.")
            .Must(op => op.Equals("add", StringComparison.OrdinalIgnoreCase) || op.Equals("remove", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Operation must be either 'add' or 'remove'.");
    }
}
