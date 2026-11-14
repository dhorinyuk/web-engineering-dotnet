using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public class ReviewsClient
{
    private readonly HttpClient _httpClient;

    public ReviewsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ReviewsSummaryDto?> GetReviewsByProductIdAsync(long productId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/api/reviews?productId={productId}&page=1&pageSize=50", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ReviewsSummaryDto(productId, 0, 0, new List<ReviewItemDto>());
        }

        response.EnsureSuccessStatusCode();

        var pagedResult = await response.Content.ReadFromJsonAsync<ReviewsPagedResult>(cancellationToken: ct);
        if (pagedResult == null || pagedResult.Items == null || pagedResult.Items.Count == 0)
        {
            return new ReviewsSummaryDto(productId, 0, 0, new List<ReviewItemDto>());
        }

        var items = pagedResult.Items;
        var totalReviews = pagedResult.TotalCount > 0 ? pagedResult.TotalCount : items.Count;
        var avgRating = items.Any() ? Math.Round(items.Average(r => r.Rating), 2) : 0;

        return new ReviewsSummaryDto(
            productId,
            totalReviews,
            avgRating,
            items.Take(5).ToList());
    }
}
