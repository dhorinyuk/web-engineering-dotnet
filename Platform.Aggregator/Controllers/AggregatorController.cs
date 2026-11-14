using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Api.Grpc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Orders.Api.Grpc;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;
using Reviews.WebApi.Grpc;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/aggregate")]
[Produces("application/json")]
public class AggregatorController : ControllerBase
{
    private readonly CatalogService.CatalogServiceClient _catalogGrpcClient;
    private readonly ReviewsService.ReviewsServiceClient _reviewsGrpcClient;
    private readonly OrdersService.OrdersServiceClient _ordersGrpcClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AggregatorController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AggregatorController(
        CatalogService.CatalogServiceClient catalogGrpcClient,
        ReviewsService.ReviewsServiceClient reviewsGrpcClient,
        OrdersService.OrdersServiceClient ordersGrpcClient,
        IDistributedCache cache,
        ILogger<AggregatorController> logger)
    {
        _catalogGrpcClient = catalogGrpcClient ?? throw new ArgumentNullException(nameof(catalogGrpcClient));
        _reviewsGrpcClient = reviewsGrpcClient ?? throw new ArgumentNullException(nameof(reviewsGrpcClient));
        _ordersGrpcClient = ordersGrpcClient ?? throw new ArgumentNullException(nameof(ordersGrpcClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Aggregates product details from Catalog API and customer ratings from Reviews API via parallel gRPC calls.
    /// Employs Redis short TTL caching, SafeGrpcAsync partial failure handling, and Task.WhenAll.
    /// </summary>
    [HttpGet("products/{id:int}")]
    [ProducesResponseType(typeof(ProductAggregateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductAggregateResponse>> GetProductAggregate(int id, CancellationToken ct)
    {
        var response = await GetProductAggregateInternal(id, ct);
        return Ok(response);
    }

    /// <summary>
    /// Aggregates order lifecycle data from Orders API and enhances items with live catalog and reviews projections via gRPC.
    /// </summary>
    [HttpGet("orders/{id:long}")]
    [HttpGet("orders/{id:long}/full")]
    [ProducesResponseType(typeof(OrderAggregateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderAggregateResponse>> GetOrderAggregate(long id, CancellationToken ct)
    {
        var cacheKey = $"aggregate:order:{id}";

        // 1. Check Redis cache before executing calls
        try
        {
            var cachedJson = await _cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                _logger.LogInformation("Returning cached aggregate for order {OrderId}", id);
                var cachedResponse = JsonSerializer.Deserialize<OrderAggregateResponse>(cachedJson, JsonOptions);
                if (cachedResponse != null)
                {
                    return Ok(cachedResponse);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cached order aggregate for key {Key}: {Message}", cacheKey, ex.Message);
        }

        // 2. Fetch order via gRPC
        var (orderData, orderWarning) = await SafeExecution.SafeGrpcAsync(
            async () => await _ordersGrpcClient.GetOrderByIdAsync(new GetOrderByIdRequest { Id = id }, cancellationToken: ct),
            "orders-api",
            _logger);

        var warnings = new List<string>();
        if (!string.IsNullOrEmpty(orderWarning)) warnings.Add(orderWarning);

        var response = new OrderAggregateResponse
        {
            OrderId = id,
            Order = orderData != null ? MapOrder(orderData) : null,
            Warnings = warnings
        };

        // 3. Fetch item product details in parallel using Task.WhenAll
        if (orderData?.Items != null && orderData.Items.Count > 0)
        {
            var productTasks = orderData.Items.Select(item =>
                GetProductAggregateInternal((int)item.ProductId, ct)
            ).ToList();

            var productResults = await Task.WhenAll(productTasks);

            foreach (var productAggregate in productResults)
            {
                if (productAggregate != null)
                {
                    response.ProductsDetails.Add(productAggregate);
                    foreach (var w in productAggregate.Warnings)
                    {
                        if (!response.Warnings.Contains(w)) response.Warnings.Add(w);
                    }
                }
            }
        }

        // 4. Cache aggregated response in Redis with short TTL (45s)
        if (response.Order != null)
        {
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45)
                };
                var serialized = JsonSerializer.Serialize(response, JsonOptions);
                await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache aggregated order {OrderId} in Redis: {Message}", id, ex.Message);
            }
        }

        return Ok(response);
    }

    private async Task<ProductAggregateResponse> GetProductAggregateInternal(int id, CancellationToken ct)
    {
        var cacheKey = $"aggregate:product:{id}";

        // 1. Check Redis cache before executing calls
        try
        {
            var cachedJson = await _cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                _logger.LogInformation("Returning cached aggregate for product {ProductId}", id);
                var cachedResponse = JsonSerializer.Deserialize<ProductAggregateResponse>(cachedJson, JsonOptions);
                if (cachedResponse != null)
                {
                    return cachedResponse;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cached product aggregate for key {Key}: {Message}", cacheKey, ex.Message);
        }

        // 2. Parallel gRPC calls via Task.WhenAll
        var catalogTask = SafeExecution.SafeGrpcAsync(
            async () => await _catalogGrpcClient.GetProductByIdAsync(new GetProductByIdRequest { Id = id }, cancellationToken: ct),
            "catalog-api",
            _logger);

        var reviewsTask = SafeExecution.SafeGrpcAsync(
            async () => await _reviewsGrpcClient.GetReviewsByProductIdAsync(new GetReviewsByProductIdRequest { ProductId = id }, cancellationToken: ct),
            "reviews-api",
            _logger);

        await Task.WhenAll(catalogTask, reviewsTask);

        var (catalogData, catalogWarning) = await catalogTask;
        var (reviewsData, reviewsWarning) = await reviewsTask;

        var warnings = new List<string>();
        if (!string.IsNullOrEmpty(catalogWarning)) warnings.Add(catalogWarning);
        if (!string.IsNullOrEmpty(reviewsWarning)) warnings.Add(reviewsWarning);

        var response = new ProductAggregateResponse
        {
            ProductId = id,
            Product = catalogData != null ? MapCatalogProduct(catalogData) : null,
            Reviews = reviewsData != null ? MapReviewsSummary(reviewsData) : null,
            Warnings = warnings
        };

        // 3. Cache aggregated response in Redis with short TTL (45s)
        if (response.Product != null || response.Reviews != null)
        {
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45)
                };
                var serialized = JsonSerializer.Serialize(response, JsonOptions);
                await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache aggregated product {ProductId} in Redis: {Message}", id, ex.Message);
            }
        }

        return response;
    }

    private static CatalogProductDto MapCatalogProduct(ProductResponse r)
    {
        return new CatalogProductDto(
            r.Id,
            r.BrandId,
            r.BrandName,
            r.Sku,
            r.Slug,
            r.Name,
            (decimal)r.Price,
            (decimal)r.CostPrice,
            (decimal)r.WeightKg,
            r.Stock,
            r.IsAvailable,
            DateTime.TryParse(r.CreatedAt, out var cAt) ? cAt : DateTime.UtcNow,
            DateTime.TryParse(r.UpdatedAt, out var uAt) ? uAt : null,
            r.Details != null
                ? new CatalogProductDetailsDto(
                    r.Details.TechnicalDescription,
                    r.Details.WarrantyMonths,
                    r.Details.CountryOfOrigin,
                    r.Details.Dimensions,
                    r.Details.InstallationGuideUrl)
                : null,
            r.Images.Select(i => new CatalogProductImageDto(i.Id, i.Url, i.DisplayOrder, i.IsMain)).ToList(),
            r.Categories.Select(c => new CatalogProductCategoryDto(
                c.CategoryId,
                c.CategoryName,
                c.CategorySlug,
                c.IsPrimary,
                DateTime.TryParse(c.AddedAt, out var aAt) ? aAt : DateTime.UtcNow)).ToList()
        );
    }

    private static ReviewsSummaryDto MapReviewsSummary(ReviewsSummaryResponse r)
    {
        return new ReviewsSummaryDto(
            r.ProductId,
            r.TotalReviews,
            r.AverageRating,
            r.RecentReviews.Select(i => new ReviewItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.AuthorName,
                i.Rating,
                i.Text,
                i.CommentsCount,
                i.Version)).ToList()
        );
    }

    private static OrderDto MapOrder(OrderResponse r)
    {
        return new OrderDto(
            r.Id,
            r.OrderNumber,
            r.CustomerId,
            r.Status,
            (decimal)r.TotalAmount,
            DateTime.TryParse(r.CreatedAt, out var dt) ? dt : DateTime.UtcNow,
            r.Items.Select(i => new OrderItemDto(
                i.ProductId,
                i.ProductName,
                (decimal)i.UnitPrice,
                i.Units,
                (decimal)i.SubTotal)).ToList()
        );
    }
}
