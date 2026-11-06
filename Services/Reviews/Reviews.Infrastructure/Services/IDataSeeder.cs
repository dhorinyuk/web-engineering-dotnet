namespace Reviews.Infrastructure.Services;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
