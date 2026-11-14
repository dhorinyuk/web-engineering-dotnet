using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// SQL Server container with secret password and persistent volume
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sqlServer = builder.AddSqlServer("sqlserver", password: sqlPassword)
    .WithDataVolume();

var ordersDb = sqlServer.AddDatabase("OrdersDb");
var catalogDb = sqlServer.AddDatabase("CatalogDb");

// MongoDB container with persistent volume
var mongo = builder.AddMongoDB("mongodb")
    .WithDataVolume();

var reviewsDb = mongo.AddDatabase("ReviewsDb");

// Redis cache container with persistent volume
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// 1. Core domain microservices
var ordersApi = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WaitFor(ordersDb)
    .WithReference(redis)
    .WaitFor(redis);

var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithReference(redis)
    .WaitFor(redis);

var reviewsApi = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WaitFor(reviewsDb)
    .WithReference(redis)
    .WaitFor(redis);

// 2. Composite Aggregator (waits for core microservices & redis)
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(ordersApi)
    .WithReference(catalogApi)
    .WithReference(reviewsApi)
    .WithReference(redis)
    .WaitFor(ordersApi)
    .WaitFor(catalogApi)
    .WaitFor(reviewsApi)
    .WaitFor(redis);

// 3. YARP API Gateway on fixed port 5000 (waits for aggregator & microservices)
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithReference(ordersApi)
    .WithReference(catalogApi)
    .WithReference(reviewsApi)
    .WithReference(aggregator)
    .WaitFor(aggregator)
    .WithHttpEndpoint(port: 5000, name: "http");

builder.Build().Run();
