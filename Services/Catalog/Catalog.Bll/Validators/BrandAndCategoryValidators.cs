using Catalog.Bll.Dtos;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class CreateBrandDtoValidator : AbstractValidator<CreateBrandDto>
{
    public CreateBrandDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Brand name is required.")
            .Length(2, 100).WithMessage("Brand name must be between 2 and 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Brand code is required.")
            .Length(2, 50).WithMessage("Brand code must be between 2 and 50 characters.")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("Brand code can only contain alphanumeric characters, hyphens, and underscores.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Length(2, 60).WithMessage("Country must be between 2 and 60 characters.");
    }
}

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .Length(2, 100).WithMessage("Category name must be between 2 and 100 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .Length(2, 120).WithMessage("Slug must be between 2 and 120 characters.")
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");

        When(x => x.ParentCategoryId.HasValue, () =>
        {
            RuleFor(x => x.ParentCategoryId!.Value)
                .GreaterThan(0).WithMessage("Parent category ID must be greater than zero.");
        });
    }
}
