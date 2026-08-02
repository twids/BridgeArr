using System.Net.Http.Json;
using System.Text.Json;
using BridgeArr.Plugins.Radarr.Models;

namespace BridgeArr.Plugins.Radarr;

/// <summary>
/// HTTP client for the Radarr API.
/// </summary>
internal class RadarrApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RadarrApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<RadarrMovie>> GetMoviesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/v3/movie", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<RadarrMovie>>(JsonOptions, cancellationToken) ?? new();
    }

    public async Task<RadarrMovie?> GetMovieAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/v3/movie/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RadarrMovie>(JsonOptions, cancellationToken);
    }

    public async Task<List<RadarrTag>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/v3/tag", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<RadarrTag>>(JsonOptions, cancellationToken) ?? new();
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
