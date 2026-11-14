using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

public class CatalogClient
{
    private readonly HttpClient _httpClient;

    public CatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CatalogProductDto?> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/api/products/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogProductDto>(cancellationToken: ct);
    }
}
