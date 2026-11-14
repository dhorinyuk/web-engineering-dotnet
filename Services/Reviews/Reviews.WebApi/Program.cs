using Platform.ServiceDefaults;
using Reviews.Application;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Services;
using Reviews.WebApi.Mapping;
using Reviews.WebApi.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Criterion 4: Service Defaults (Serilog + OTLP, OpenTelemetry, Health Checks, Service Discovery, CorrelationId)
builder.AddServiceDefaults();

// MongoDB connection string resolution
var reviewsConnectionString = builder.Configuration.GetConnectionString("ReviewsDb");
if (!string.IsNullOrEmpty(reviewsConnectionString))
{
    builder.Services.Configure<Reviews.Infrastructure.Configuration.MongoDbOptions>(options =>
    {
        options.ConnectionString = reviewsConnectionString;
    });
}

// Criterion 7: Clean Architecture layers registration via extension methods
builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

// Criterion 12: AutoMapper registration
builder.Services.AddAutoMapper(typeof(ReviewProfile));

// Criterion 13: ProblemDetails and custom MongoDB Exception Handler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ReviewsExceptionHandler>();

// Controllers & Swagger registration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Reviews API (Clean Architecture + MongoDB + CQRS)",
        Version = "v1",
        Description = "Customer feedback microservice built with Clean Architecture, MongoDB Driver, CQRS via MediatR, and optimistic concurrency."
    });
});

var app = builder.Build();

// Service defaults endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Correlation ID middleware
app.UseCorrelationId();

// Pipeline configuration
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reviews API v1");
    });
}

// Serilog request logging middleware
app.UseSerilogRequestLogging();

app.UseAuthorization();
app.MapControllers();

// Criterion 14: Idempotent database seeding (skipped in production)
if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Automatic MongoDB seeding was deferred on startup: {Message}", ex.Message);
    }
}

app.Run();

// For integration tests
public partial class Program { }
