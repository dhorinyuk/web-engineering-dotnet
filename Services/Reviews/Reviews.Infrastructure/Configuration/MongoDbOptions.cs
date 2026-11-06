namespace Reviews.Infrastructure.Configuration;

public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "ReviewsServiceDb";
    public int ServerSelectionTimeoutMs { get; set; } = 5000;
    public int ConnectTimeoutMs { get; set; } = 10000;
    public int MaxConnectionPoolSize { get; set; } = 100;
    public int MinConnectionPoolSize { get; set; } = 10;
}
