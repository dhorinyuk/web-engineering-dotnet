using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public class OrdersClient
{
    private readonly HttpClient _httpClient;

    public OrdersClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OrderDto?> GetOrderByIdAsync(long id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/api/orders/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
    }
}
