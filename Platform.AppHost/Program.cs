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

// 1. Core domain microservices
var ordersApi = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WaitFor(ordersDb);

var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

var reviewsApi = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WaitFor(reviewsDb);

// 2. Composite Aggregator (waits for core microservices)
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(ordersApi)
    .WithReference(catalogApi)
    .WithReference(reviewsApi)
    .WaitFor(ordersApi)
    .WaitFor(catalogApi)
    .WaitFor(reviewsApi);

// 3. YARP API Gateway on fixed port 5000 (waits for aggregator & microservices)
builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithReference(ordersApi)
    .WithReference(catalogApi)
    .WithReference(reviewsApi)
    .WithReference(aggregator)
    .WaitFor(aggregator)
    .WithHttpEndpoint(port: 5000, name: "http");

builder.Build().Run();
