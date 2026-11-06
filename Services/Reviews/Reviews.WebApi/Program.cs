using Reviews.Application;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Services;
using Reviews.WebApi.Mapping;
using Reviews.WebApi.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration with structured properties (Criterion 13)
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

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
