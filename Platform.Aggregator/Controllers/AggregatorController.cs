using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/aggregate")]
[Produces("application/json")]
public class AggregatorController : ControllerBase
{
    private readonly CatalogClient _catalogClient;
    private readonly ReviewsClient _reviewsClient;
    private readonly OrdersClient _ordersClient;
    private readonly ILogger<AggregatorController> _logger;

    public AggregatorController(
        CatalogClient catalogClient,
        ReviewsClient reviewsClient,
        OrdersClient ordersClient,
        ILogger<AggregatorController> logger)
    {
        _catalogClient = catalogClient;
        _reviewsClient = reviewsClient;
        _ordersClient = ordersClient;
        _logger = logger;
    }

    /// <summary>
    /// Aggregates product details from Catalog API and customer ratings from Reviews API in parallel.
    /// Employs SafeAsync partial failure handling and Task.WhenAll.
    /// </summary>
    [HttpGet("products/{id:int}")]
    [ProducesResponseType(typeof(ProductAggregateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductAggregateResponse>> GetProductAggregate(int id, CancellationToken ct)
    {
        var catalogTask = SafeExecution.SafeAsync(() => _catalogClient.GetProductByIdAsync(id, ct), "catalog-api", _logger);
        var reviewsTask = SafeExecution.SafeAsync(() => _reviewsClient.GetReviewsByProductIdAsync(id, ct), "reviews-api", _logger);

        await Task.WhenAll(catalogTask, reviewsTask);

        var (catalogData, catalogWarning) = await catalogTask;
        var (reviewsData, reviewsWarning) = await reviewsTask;

        var warnings = new List<string>();
        if (!string.IsNullOrEmpty(catalogWarning)) warnings.Add(catalogWarning);
        if (!string.IsNullOrEmpty(reviewsWarning)) warnings.Add(reviewsWarning);

        var response = new ProductAggregateResponse
        {
            ProductId = id,
            Product = catalogData,
            Reviews = reviewsData,
            Warnings = warnings
        };

        return Ok(response);
    }

    /// <summary>
    /// Aggregates order lifecycle data from Orders API and enhances items with live catalog and reviews projections.
    /// </summary>
    [HttpGet("orders/{id:long}")]
    [HttpGet("orders/{id:long}/full")]
    [ProducesResponseType(typeof(OrderAggregateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderAggregateResponse>> GetOrderAggregate(long id, CancellationToken ct)
    {
        var (orderData, orderWarning) = await SafeExecution.SafeAsync(() => _ordersClient.GetOrderByIdAsync(id, ct), "orders-api", _logger);

        var warnings = new List<string>();
        if (!string.IsNullOrEmpty(orderWarning)) warnings.Add(orderWarning);

        var response = new OrderAggregateResponse
        {
            OrderId = id,
            Order = orderData,
            Warnings = warnings
        };

        if (orderData?.Items != null && orderData.Items.Count > 0)
        {
            var productTasks = orderData.Items.Select(item =>
                GetProductAggregate((int)item.ProductId, ct)
            ).ToList();

            var productResults = await Task.WhenAll(productTasks);

            foreach (var actionResult in productResults)
            {
                if (actionResult.Result is OkObjectResult ok && ok.Value is ProductAggregateResponse productAggregate)
                {
                    response.ProductsDetails.Add(productAggregate);
                    if (productAggregate.Warnings.Count > 0)
                    {
                        foreach (var w in productAggregate.Warnings)
                        {
                            if (!response.Warnings.Contains(w)) response.Warnings.Add(w);
                        }
                    }
                }
            }
        }

        return Ok(response);
    }
}
