using System.Net.Http.Json;
using System.Text.Json;
using BridgeArr.Plugins.Sonarr.Models;

namespace BridgeArr.Plugins.Sonarr;

/// <summary>
/// HTTP client for the Sonarr API.
/// </summary>
internal class SonarrApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SonarrApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SonarrSeries>> GetSeriesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/v3/series", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<SonarrSeries>>(JsonOptions, cancellationToken) ?? new();
    }

    public async Task<SonarrSeries?> GetSeriesByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/v3/series/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SonarrSeries>(JsonOptions, cancellationToken);
    }

    public async Task<List<SonarrTag>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/v3/tag", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<SonarrTag>>(JsonOptions, cancellationToken) ?? new();
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v3/system/status", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
