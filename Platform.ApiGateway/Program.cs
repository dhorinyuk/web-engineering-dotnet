using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.ServiceDefaults;
using Serilog;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

// 1. Service Defaults (Serilog, OpenTelemetry, Service Discovery, Health Checks, CorrelationId)
builder.AddServiceDefaults();

// 2. YARP Reverse Proxy with Service Discovery
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

// 3. Health Checks & Default Endpoints
app.MapDefaultEndpoints();

// 4. Correlation ID Middleware (before Reverse Proxy)
app.UseCorrelationId();

// 5. Routing Diagnostic Logging Middleware
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    var method = context.Request.Method;
    var path = context.Request.Path;

    await next();

    stopwatch.Stop();

    var endpoint = context.GetEndpoint();
    var routeModel = endpoint?.Metadata.GetMetadata<RouteModel>();
    var routeId = routeModel?.Config.RouteId ?? "none";
    var clusterId = routeModel?.Cluster?.ClusterId ?? "none";
    var statusCode = context.Response.StatusCode;

    Log.Information(
        "Gateway Routing: {Method} {Path} -> Route: {RouteId}, Cluster: {ClusterId} | Status: {StatusCode} in {ElapsedMs} ms",
        method, path, routeId, clusterId, statusCode, stopwatch.ElapsedMilliseconds);
});

// 6. Reverse Proxy Mapping
app.MapReverseProxy();

app.Run();

public partial class Program { }
