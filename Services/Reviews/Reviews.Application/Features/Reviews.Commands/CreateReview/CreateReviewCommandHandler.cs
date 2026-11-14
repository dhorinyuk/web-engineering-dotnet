using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Platform.ServiceDefaults.Caching;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Entities;
using Reviews.Domain.Repositories;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, string>
{
    private static readonly ActivitySource ActivitySource = new("Reviews.WebApi");
    private readonly IReviewRepository _repository;
    private readonly ITwoTierCacheService? _cache;
    private readonly IMemoryCache? _memoryCache;
    private readonly ILogger<CreateReviewCommandHandler>? _logger;

    public CreateReviewCommandHandler(
        IReviewRepository repository,
        ITwoTierCacheService? cache = null,
        IMemoryCache? memoryCache = null,
        ILogger<CreateReviewCommandHandler>? logger = null)
    {
        _repository = repository;
        _cache = cache;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public CreateReviewCommandHandler(IReviewRepository repository, ILogger<CreateReviewCommandHandler>? logger)
        : this(repository, null, null, logger)
    {
    }

    public async Task<string> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("CreateReviewBusinessLogic");
        activity?.SetTag("product.id", request.ProductId);
        activity?.SetTag("review.rating", request.Rating);

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
        _logger?.LogInformation("Review {ReviewId} created for product {ProductId} with rating {Rating}", review.Id, request.ProductId, request.Rating);
        activity?.SetTag("review.id", review.Id.ToString());

        // Cache invalidation
        _memoryCache?.Remove($"reviews:product:{request.ProductId}");
        if (_cache != null)
        {
            await _cache.RemoveAsync($"reviews:product:{request.ProductId}", "Review created", cancellationToken);
        }
        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", $"reviews:product:{request.ProductId}", "Review created");

        return review.Id.ToString();
    }
}
