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

        When(x => !string.IsNullOrWhiteSpace(x.Text), () =>
        {
            RuleFor(x => x.Text!)
                .Length(5, 4000).WithMessage("Text must be between 5 and 4000 characters.");
        });
    }
}
