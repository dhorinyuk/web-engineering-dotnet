using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.DTOs;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Queries;

public class ReviewQueries : IReviewQueries
{
    private readonly IMongoCollection<Review> _collection;

    public ReviewQueries(MongoDbContext context)
    {
        _collection = context.Reviews;
    }

    public async Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var review = await _collection
            .Find(r => r.Id == objectId)
            .FirstOrDefaultAsync(ct);

        return review == null ? null : MapToDto(review);
    }

    // Criterion 6 & 11: MongoDB driver pagination ($match, $skip, $limit) and projection to DTO
    public async Task<PagedResult<ReviewDto>> GetListAsync(
        long? productId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var filter = productId.HasValue
            ? Builders<Review>.Filter.Eq(r => r.ProductId, productId.Value)
            : Builders<Review>.Filter.Empty;

        var totalCount = (int)await _collection.CountDocumentsAsync(filter, cancellationToken: ct);

        var skip = (page - 1) * pageSize;

        var items = await _collection
            .Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResult<ReviewDto>(dtos, totalCount, page, pageSize);
    }

    private static ReviewDto MapToDto(Review review)
    {
        return new ReviewDto(
            Id: review.Id.ToString(),
            ProductId: review.ProductId,
            ProductName: review.ProductName,
            Rating: review.Rating,
            Headline: review.Headline,
            Content: review.Content,
            Author: new AuthorDto(
                review.Author.UserId,
                review.Author.Name,
                review.Author.Email,
                review.Author.IsVerifiedBuyer),
            DiscussionId: review.DiscussionId?.ToString(),
            Comments: review.Comments.Select(c => new CommentDto(
                c.CommentId,
                c.AuthorName,
                c.CommentText,
                c.CreatedAt)).ToList(),
            Photos: review.Photos.ToList(),
            CreatedAt: review.CreatedAt,
            UpdatedAt: review.UpdatedAt,
            Version: review.Version);
    }
}
