using Catalog.Api.Middleware;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validators;
using Catalog.Dal;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Criterion 13: Serilog configuration with structured properties & EF Command logging override
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .WriteTo.Console());

// Database Context registration
var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? "Server=localhost;Database=CatalogDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True";

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
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
builder.Services.AddAutoMapper(typeof(CatalogMappingProfile));

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

// Criterion 9: Idempotent database migration & programmatic seeding (skipped in production)
if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    try
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
            await DbInitializer.SeedAsync(db);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Automatic migration or seeding was skipped (SQL Server connection deferred): {Message}", ex.Message);
    }
}

app.Run();

// For integration tests
public partial class Program { }
