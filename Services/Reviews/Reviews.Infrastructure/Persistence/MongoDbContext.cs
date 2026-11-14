using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Configuration;

namespace Reviews.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var opt = options.Value ?? throw new ArgumentNullException(nameof(options));

        var settings = MongoClientSettings.FromConnectionString(opt.ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(opt.ServerSelectionTimeoutMs);
        settings.ConnectTimeout = TimeSpan.FromMilliseconds(opt.ConnectTimeoutMs);
        settings.MaxConnectionPoolSize = opt.MaxConnectionPoolSize;
        settings.MinConnectionPoolSize = opt.MinConnectionPoolSize;

        _client = new MongoClient(settings);
        _database = _client.GetDatabase(opt.DatabaseName);
    }

    public IMongoClient Client => _client;
    public IMongoDatabase Database => _database;

    public IMongoCollection<Review> Reviews => _database.GetCollection<Review>("reviews");
}
