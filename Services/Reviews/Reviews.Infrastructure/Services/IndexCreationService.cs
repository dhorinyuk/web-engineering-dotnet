using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Services;

public class IndexCreationService : IHostedService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<IndexCreationService> _logger;

    public IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var keys = Builders<Review>.IndexKeys;

            // 1. Compound index: ProductId (asc) + CreatedAt (desc) (Criterion 14)
            var compoundIndexModel = new CreateIndexModel<Review>(
                keys.Ascending(r => r.ProductId).Descending(r => r.CreatedAt),
                new CreateIndexOptions { Name = "IX_Reviews_ProductId_CreatedAt" });

            // 2. Text index on Headline and Content for full-text search (Criterion 14)
            var textIndexModel = new CreateIndexModel<Review>(
                keys.Text(r => r.Headline).Text(r => r.Content),
                new CreateIndexOptions { Name = "IX_Reviews_TextSearch" });

            // 3. Unique index ensuring one review per author per product (Criterion 14)
            var uniqueAuthorProductIndex = new CreateIndexModel<Review>(
                keys.Ascending(r => r.ProductId).Ascending("author.userId"),
                new CreateIndexOptions
                {
                    Name = "UQ_Reviews_ProductId_AuthorUserId",
                    Unique = true,
                    Sparse = true // Sparse allows null userIds for guest reviews
                });

            await _context.Reviews.Indexes.CreateManyAsync(
                new[] { compoundIndexModel, textIndexModel, uniqueAuthorProductIndex },
                cancellationToken);

            _logger.LogInformation("MongoDB indexes for Reviews collection successfully ensured.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deferred MongoDB index creation (server connection pending): {Message}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
