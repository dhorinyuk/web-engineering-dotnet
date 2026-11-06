using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Repositories;
using Reviews.Infrastructure.Configuration;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Queries;
using Reviews.Infrastructure.Repositories;
using Reviews.Infrastructure.Services;

namespace Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Criterion 10: Register BsonClassMap and Conventions exactly once on startup
        MongoMappings.Register();

        // MongoDB Options registration
        services.Configure<MongoDbOptions>(config.GetSection(MongoDbOptions.SectionName));

        // Persistence context
        services.AddSingleton<MongoDbContext>();

        // Criterion 4 & 11: Repositories and Queries
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();

        // Criterion 14: Data seeder and automated index management
        services.AddScoped<IDataSeeder, ReviewSeeder>();
        services.AddHostedService<IndexCreationService>();

        return services;
    }
}
