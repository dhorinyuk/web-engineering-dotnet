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
    private readonly IMongoCollection<Review> _reviews;

    public ReviewQueries(MongoDbContext context)
    {
        _reviews = context.Reviews;
    }

    public async Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var projection = Builders<Review>.Projection.Expression(r => new ReviewDto(
            r.Id.ToString(),
            r.ProductId,
            r.ProductName,
            r.Author.DisplayName,
            r.Rating,
            r.Text,
            r.Comments.Count,
            r.Version
        ));

        return await _reviews
            .Find(r => r.Id == objectId)
            .Project(projection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<ReviewDto>> GetListAsync(
        long? productId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var filter = productId.HasValue
            ? Builders<Review>.Filter.Eq(r => r.ProductId, productId.Value)
            : Builders<Review>.Filter.Empty;

        var totalCount = (int)await _reviews.CountDocumentsAsync(filter, cancellationToken: ct);

        var projection = Builders<Review>.Projection.Expression(r => new ReviewDto(
            r.Id.ToString(),
            r.ProductId,
            r.ProductName,
            r.Author.DisplayName,
            r.Rating,
            r.Text,
            r.Comments.Count,
            r.Version
        ));

        var items = await _reviews.Find(filter)
            .Project(projection)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ReviewDto>(items, totalCount, page, pageSize);
    }
}
