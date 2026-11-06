using FluentValidation;
using MongoDB.Bson;
using Reviews.Domain.Repositories;

namespace Reviews.Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    private readonly IReviewRepository _repository;

    public CreateReviewCommandValidator(IReviewRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId must be greater than zero.");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName is required.")
            .Length(2, 250).WithMessage("ProductName must be between 2 and 250 characters.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Headline)
            .NotEmpty().WithMessage("Headline is required.")
            .Length(3, 200).WithMessage("Headline must be between 3 and 200 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .Length(5, 4000).WithMessage("Content must be between 5 and 4000 characters.");

        RuleFor(x => x.AuthorName)
            .NotEmpty().WithMessage("AuthorName is required.")
            .Length(2, 100).WithMessage("AuthorName must be between 2 and 100 characters.");

        RuleFor(x => x.AuthorEmail)
            .NotEmpty().WithMessage("AuthorEmail is required.")
            .EmailAddress().WithMessage("AuthorEmail must be a valid email address.")
            .MaximumLength(255).WithMessage("AuthorEmail cannot exceed 255 characters.");

        When(x => !string.IsNullOrWhiteSpace(x.DiscussionId), () =>
        {
            RuleFor(x => x.DiscussionId)
                .Must(id => ObjectId.TryParse(id, out _))
                .WithMessage("DiscussionId must be a valid 24-character hexadecimal ObjectId.");
        });

        // Async business rule via MustAsync: author cannot submit duplicate review for the same product (Criterion 8)
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                if (string.IsNullOrWhiteSpace(cmd.AuthorUserId))
                    return true;

                var exists = await _repository.ExistsAsync(cmd.ProductId, cmd.AuthorUserId, ct);
                return !exists;
            })
            .WithMessage("Author has already submitted a review for this product.");
    }
}
