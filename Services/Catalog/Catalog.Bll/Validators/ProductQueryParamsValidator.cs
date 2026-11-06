using Catalog.Bll.Common;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class ProductQueryParamsValidator : AbstractValidator<ProductQueryParams>
{
    private static readonly string[] AllowedSortFields = { "price", "name", "createdat" };

    public ProductQueryParamsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        When(x => x.MinPrice.HasValue, () =>
        {
            RuleFor(x => x.MinPrice!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("MinPrice cannot be negative.");
        });

        When(x => x.MaxPrice.HasValue, () =>
        {
            RuleFor(x => x.MaxPrice!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("MaxPrice cannot be negative.");

            When(x => x.MinPrice.HasValue, () =>
            {
                RuleFor(x => x)
                    .Must(x => x.MaxPrice!.Value >= x.MinPrice!.Value)
                    .WithMessage("MaxPrice must be greater than or equal to MinPrice.");
            });
        });

        When(x => !string.IsNullOrWhiteSpace(x.SortBy), () =>
        {
            RuleFor(x => x.SortBy)
                .Must(sort => AllowedSortFields.Contains(sort!.ToLowerInvariant()))
                .WithMessage($"SortBy can only be one of the following: {string.Join(", ", AllowedSortFields)}.");
        });
    }
}
