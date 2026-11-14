using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Entities;
using Reviews.Domain.Repositories;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, string>
{
    private readonly IReviewRepository _repository;

    public CreateReviewCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var author = new Author(
            request.AuthorUserId,
            request.AuthorName,
            request.AuthorEmail,
            request.IsVerifiedBuyer);

        ObjectId? discussionId = null;
        if (!string.IsNullOrWhiteSpace(request.DiscussionId) && ObjectId.TryParse(request.DiscussionId, out var parsedDiscussionId))
        {
            discussionId = parsedDiscussionId;
        }

        var review = new Review(
            request.ProductId,
            request.ProductName,
            author,
            request.Rating,
            request.Text,
            discussionId,
            request.Photos);

        await _repository.AddAsync(review, cancellationToken);

        return review.Id.ToString();
    }
}
