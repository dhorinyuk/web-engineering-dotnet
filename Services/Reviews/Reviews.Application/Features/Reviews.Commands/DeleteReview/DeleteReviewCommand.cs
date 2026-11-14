using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Repositories;

namespace Reviews.Application.Features.Reviews.Commands.DeleteReview;

public record DeleteReviewCommand(string Id) : ICommand;

public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand>
{
    private readonly IReviewRepository _repository;

    public DeleteReviewCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(request.Id, out var objectId))
            throw new EntityNotFoundException("Review", request.Id);

        var existing = await _repository.GetByIdAsync(objectId, cancellationToken);
        if (existing == null)
            throw new EntityNotFoundException("Review", request.Id);

        await _repository.DeleteAsync(objectId, cancellationToken);
    }
}

public class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Review Id is required.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Review Id must be a valid 24-character hexadecimal ObjectId.");
    }
}
