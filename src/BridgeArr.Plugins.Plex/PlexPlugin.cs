using System.Text.Json;
using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Plugins.Plex;

/// <summary>
/// Plex plugin implementing IMediaTarget.
/// </summary>
public class PlexPlugin : IMediaTarget
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PlexPlugin> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string PluginType => "plex";
    public string DisplayName => "Plex";
    public string Version => "1.0.0";
    public PluginCapabilities Capabilities => PluginCapabilities.MediaTarget;

    public PlexPlugin(IHttpClientFactory httpClientFactory, ILogger<PlexPlugin> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private PlexApiClient CreateClient(string configurationJson)
    {
        var config = JsonSerializer.Deserialize<PlexConfiguration>(configurationJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid Plex configuration.");

        var httpClient = _httpClientFactory.CreateClient("plex");
        httpClient.BaseAddress = new Uri(config.Url.TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Remove("X-Plex-Token");
        httpClient.DefaultRequestHeaders.Remove("Accept");
        httpClient.DefaultRequestHeaders.Add("X-Plex-Token", config.Token);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/xml");
        return new PlexApiClient(httpClient);
    }

    public async Task<bool> TestConnectionAsync(string configurationJson, CancellationToken cancellationToken = default)
    {
        try
        {
            return await CreateClient(configurationJson).TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plex connection test failed.");
            return false;
        }
    }

    public async Task<string?> FindMediaAsync(MediaItem mediaItem, string configurationJson, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(configurationJson);
        return mediaItem.Type switch
        {
            MediaType.Movie => await client.SearchMovieAsync(mediaItem.Title, mediaItem.Year ?? 0, cancellationToken),
            MediaType.Series => await client.SearchSeriesAsync(mediaItem.Title, mediaItem.Year ?? 0, cancellationToken),
            _ => null
        };
    }

    public async Task<IReadOnlyList<string>> GetLabelsAsync(string targetItemId, string configurationJson, CancellationToken cancellationToken = default)
        => await CreateClient(configurationJson).GetLabelsAsync(targetItemId, cancellationToken);

    public async Task<bool> SetLabelsAsync(string targetItemId, IReadOnlyList<string> labels, string configurationJson, CancellationToken cancellationToken = default)
        => await CreateClient(configurationJson).SetLabelsAsync(targetItemId, labels, cancellationToken);

    public async Task<bool> AddToCollectionAsync(string targetItemId, string collectionName, string configurationJson, CancellationToken cancellationToken = default)
        => await CreateClient(configurationJson).AddToCollectionAsync(targetItemId, collectionName, cancellationToken);
}
