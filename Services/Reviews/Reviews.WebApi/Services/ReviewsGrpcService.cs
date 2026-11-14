using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Reviews.Application.DTOs;
using Reviews.Application.Features.Reviews.Queries.GetReviewsList;
using Reviews.WebApi.Grpc;

namespace Reviews.WebApi.Services;

public class ReviewsGrpcService : ReviewsService.ReviewsServiceBase
{
    private readonly ISender _mediator;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReviewsGrpcService> _logger;

    public ReviewsGrpcService(
        ISender mediator,
        IMemoryCache cache,
        ILogger<ReviewsGrpcService> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<ReviewsSummaryResponse> GetReviewsByProductId(GetReviewsByProductIdRequest request, ServerCallContext context)
    {
        if (request.ProductId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Product ID must be greater than zero. Received: {request.ProductId}"));
        }

        try
        {
            var pagedResult = await _mediator.Send(new GetReviewsListQuery(request.ProductId, 1, 10), context.CancellationToken);
            var items = pagedResult.Items ?? new List<ReviewDto>();

            var totalReviews = pagedResult.TotalCount;
            var averageRating = items.Count > 0 ? items.Average(r => r.Rating) : 0.0;

            var response = new ReviewsSummaryResponse
            {
                ProductId = request.ProductId,
                TotalReviews = totalReviews,
                AverageRating = Math.Round(averageRating, 2)
            };

            foreach (var item in items)
            {
                response.RecentReviews.Add(new ReviewItemResponse
                {
                    Id = item.Id ?? string.Empty,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName ?? string.Empty,
                    AuthorName = item.AuthorName ?? string.Empty,
                    Rating = item.Rating,
                    Text = item.Text ?? string.Empty,
                    CommentsCount = item.CommentsCount,
                    Version = item.Version
                });
            }

            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument in Reviews gRPC: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            _logger.LogError(ex, "Unexpected error retrieving reviews for product {ProductId}: {Message}", request.ProductId, ex.Message);
            throw new RpcException(new Status(StatusCode.Internal, $"An internal error occurred: {ex.Message}"));
        }
    }
}
