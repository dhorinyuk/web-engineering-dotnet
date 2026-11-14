using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Repositories;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly IMongoCollection<Review> _reviews;

    public ReviewRepository(MongoDbContext context)
    {
        _reviews = context.Reviews;
    }

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
    {
        return await _reviews
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> ExistsAsync(long productId, string? authorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authorUserId))
            return false;

        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.ProductId, productId),
            Builders<Review>.Filter.Eq(r => r.Author.UserId, authorUserId)
        );

        return await _reviews.Find(filter).AnyAsync(ct);
    }

    public async Task AddAsync(Review review, CancellationToken ct = default)
    {
        await _reviews.InsertOneAsync(review, cancellationToken: ct);
    }

    // Criterion 11: Optimistic concurrency control via Version field check
    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var expectedVersion = review.Version;
        review.BumpVersion();

        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, review.Id),
            Builders<Review>.Filter.Eq(r => r.Version, expectedVersion)
        );

        var result = await _reviews.ReplaceOneAsync(
            filter,
            review,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken: ct);

        if (result.MatchedCount == 0)
        {
            throw new ConflictException(
                $"Review '{review.Id}' was modified by another concurrent operation. Expected version {expectedVersion}, but document state changed.");
        }
    }

    public Task DeleteAsync(ObjectId id, CancellationToken ct = default) => _reviews.DeleteOneAsync(r => r.Id == id, ct);
}
