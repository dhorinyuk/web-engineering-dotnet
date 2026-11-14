using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Catalog.Api.Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Api.Grpc;
using Platform.Aggregator.Clients;
using Platform.ServiceDefaults;
using Reviews.WebApi.Grpc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Service Defaults (Serilog + OTLP, OpenTelemetry, Service Discovery, Health Checks, CorrelationId)
builder.AddServiceDefaults();

// Caching: L1 Memory Cache (SizeLimit = 10_000) & L2 Redis Distributed Cache
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
builder.AddRedisDistributedCache("redis");

// 2. JSON Serialization Configuration (camelCase, ignore nulls)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// 3. Typed gRPC Clients with Service Discovery (https://[service-name])
builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o =>
{
    o.Address = new Uri("https://catalog-api");
});

builder.Services.AddGrpcClient<OrdersService.OrdersServiceClient>(o =>
{
    o.Address = new Uri("https://orders-api");
});

builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o =>
{
    o.Address = new Uri("https://reviews-api");
});

// Backward-compatible Typed HttpClients with Service Discovery
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https+http://catalog-api");
});

builder.Services.AddHttpClient<ReviewsClient>(client =>
{
    client.BaseAddress = new Uri("https+http://reviews-api");
});

builder.Services.AddHttpClient<OrdersClient>(client =>
{
    client.BaseAddress = new Uri("https+http://orders-api");
});

// 4. API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Platform Aggregator API",
        Version = "v1",
        Description = "Composite BFF / Aggregator service orchestrating parallel calls to microservices with partial degradation handling."
    });
});

var app = builder.Build();

// 5. Default Endpoints & Middleware Pipeline
app.MapDefaultEndpoints();
app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
