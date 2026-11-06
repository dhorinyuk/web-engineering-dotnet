using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.DTOs;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Repositories;

namespace Reviews.Application.Features.Reviews.Commands.AddComment;

public record AddCommentCommand(
    string ReviewId,
    string AuthorName,
    string CommentText) : ICommand<CommentDto>;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly IReviewRepository _repository;

    public AddCommentCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(request.ReviewId, out var objectId))
            throw new EntityNotFoundException("Review", request.ReviewId);

        var review = await _repository.GetByIdAsync(objectId, cancellationToken);
        if (review == null)
            throw new EntityNotFoundException("Review", request.ReviewId);

        var comment = review.AddComment(request.AuthorName, request.CommentText);

        await _repository.UpdateAsync(review, cancellationToken);

        return new CommentDto(
            comment.CommentId,
            comment.AuthorName,
            comment.CommentText,
            comment.CreatedAt);
    }
}

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty().WithMessage("ReviewId is required.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("ReviewId must be a valid 24-character hexadecimal ObjectId.");

        RuleFor(x => x.AuthorName)
            .NotEmpty().WithMessage("AuthorName is required.")
            .Length(2, 100).WithMessage("AuthorName must be between 2 and 100 characters.");

        RuleFor(x => x.CommentText)
            .NotEmpty().WithMessage("CommentText is required.")
            .Length(2, 1000).WithMessage("CommentText must be between 2 and 1000 characters.");
    }
}
