using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Api.Middleware;
using Orders.Bll.Mapping;
using Orders.Bll.Services;
using Orders.Dal;
using Platform.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Criterion 4: Service Defaults (Serilog + OTLP, OpenTelemetry, Health Checks, Service Discovery, CorrelationId)
builder.AddServiceDefaults();

// 2. Database Connection & Unit of Work (Scoped)
var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException("Connection string 'OrdersDb' is not configured.");

builder.Services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(connectionString));

// 3. Business Layer Services
builder.Services.AddScoped<IOrderService, OrderService>();

// 4. AutoMapper
builder.Services.AddAutoMapper(typeof(OrderProfile));

// 5. Exception Handling & ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// 6. Controllers & API Explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Orders API (ADO.NET + Dapper)", Version = "v1" });
});

var app = builder.Build();

// Service defaults endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Correlation ID middleware
app.UseCorrelationId();

// 7. HTTP Request Pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();

app.Run();

// For integration tests
public partial class Program { }
