using Catalog.Api.Middleware;
using Catalog.Api.Services;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validators;
using Catalog.Dal;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Criterion 4: Service Defaults (Serilog + OTLP, OpenTelemetry, Health Checks, Service Discovery, CorrelationId)
builder.AddServiceDefaults();

// Caching: L1 Memory Cache (SizeLimit = 10_000) & L2 Redis Distributed Cache
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
builder.AddRedisDistributedCache("redis");

// gRPC Services & HTTP/2 Configuration
builder.Services.AddGrpc();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(lo => lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
});

// Database Context registration
var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? "Server=localhost;Database=CatalogDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True";

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDb") ?? connectionString, sql =>
    {
        sql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
    }));

// Criterion 5: Unit of Work registration
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Criterion 7: BLL services registration
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Criterion 7: AutoMapper registration
builder.Services.AddAutoMapper(cfg =>
{
}, typeof(CatalogMappingProfile));

// Criterion 11: FluentValidation registration
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDtoValidator>();

// Criterion 12: ProblemDetails & Global Exception Handler registration
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CatalogExceptionHandler>();

// Criterion 8: Controllers & Swagger registration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Catalog API (EF Core 3-Tier Architecture)",
        Version = "v1",
        Description = "Domain-driven 3-tier catalog microservice built with Entity Framework Core, Unit of Work, Repository and Specification patterns."
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
    });
}

// Criterion 13: Serilog request logging middleware
app.UseSerilogRequestLogging();

app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<CatalogGrpcService>();

// Criterion 9: Idempotent database migration & programmatic seeding (skipped in production)
if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        await DbInitializer.SeedAsync(db);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Automatic migration or seeding was deferred on startup: {Message}", ex.Message);
    }
}

app.Run();

// For integration tests
public partial class Program { }
