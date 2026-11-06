using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Repositories;

namespace Reviews.Application.Features.Reviews.Commands.UpdateReview;

public record UpdateReviewCommand(
    string Id,
    int Rating,
    string? Headline = null,
    string? Content = null) : ICommand;

public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand>
{
    private readonly IReviewRepository _repository;

    public UpdateReviewCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(request.Id, out var objectId))
            throw new EntityNotFoundException("Review", request.Id);

        var review = await _repository.GetByIdAsync(objectId, cancellationToken);
        if (review == null)
            throw new EntityNotFoundException("Review", request.Id);

        review.UpdateRating(request.Rating, request.Headline, request.Content);

        await _repository.UpdateAsync(review, cancellationToken);
    }
}
