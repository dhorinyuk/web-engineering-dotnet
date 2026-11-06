using FluentValidation;
using MongoDB.Bson;

namespace Reviews.Application.Features.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Review Id is required.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Review Id must be a valid 24-character hexadecimal ObjectId.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        When(x => !string.IsNullOrWhiteSpace(x.Headline), () =>
        {
            RuleFor(x => x.Headline)
                .Length(3, 200).WithMessage("Headline must be between 3 and 200 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Content), () =>
        {
            RuleFor(x => x.Content)
                .Length(5, 4000).WithMessage("Content must be between 5 and 4000 characters.");
        });
    }
}
