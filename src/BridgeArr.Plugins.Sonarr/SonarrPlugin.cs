using System.Text.Json;
using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Abstractions;
using BridgeArr.Plugins.Sonarr.Models;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Plugins.Sonarr;

/// <summary>
/// Sonarr plugin implementing IMediaSource and IWebhookHandler.
/// </summary>
public class SonarrPlugin : IMediaSource, IWebhookHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SonarrPlugin> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string PluginType => "sonarr";
    public string DisplayName => "Sonarr";
    public string Version => "1.0.0";
    public string Source => "sonarr";
    public PluginCapabilities Capabilities => PluginCapabilities.MediaSource | PluginCapabilities.WebhookHandler;

    public SonarrPlugin(IHttpClientFactory httpClientFactory, ILogger<SonarrPlugin> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private SonarrApiClient CreateClient(string configurationJson)
    {
        var config = JsonSerializer.Deserialize<SonarrConfiguration>(configurationJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid Sonarr configuration.");

        var httpClient = _httpClientFactory.CreateClient("sonarr");
        httpClient.BaseAddress = new Uri(config.Url.TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
        return new SonarrApiClient(httpClient);
    }

    public async Task<bool> TestConnectionAsync(string configurationJson, CancellationToken cancellationToken = default)
    {
        try
        {
            return await CreateClient(configurationJson).TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sonarr connection test failed.");
            return false;
        }
    }

    public async Task<IReadOnlyList<MediaItem>> GetAllMediaAsync(string configurationJson, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(configurationJson);
        var series = await client.GetSeriesAsync(cancellationToken);
        var tags = await client.GetTagsAsync(cancellationToken);
        var tagMap = tags.ToDictionary(t => t.Id, t => t.Label);
        return series.Select(s => MapToMediaItem(s, tagMap)).ToList();
    }

    public async Task<MediaItem?> GetMediaByIdAsync(string sourceId, string configurationJson, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(sourceId, out var id))
        {
            return null;
        }

        var client = CreateClient(configurationJson);
        var series = await client.GetSeriesByIdAsync(id, cancellationToken);
        if (series is null)
        {
            return null;
        }

        var tags = await client.GetTagsAsync(cancellationToken);
        var tagMap = tags.ToDictionary(t => t.Id, t => t.Label);
        return MapToMediaItem(series, tagMap);
    }

    public async Task<IReadOnlyList<Tag>> GetTagsAsync(string configurationJson, CancellationToken cancellationToken = default)
    {
        var sonarrTags = await CreateClient(configurationJson).GetTagsAsync(cancellationToken);
        return sonarrTags.Select(t => new Tag { Name = t.Label }).ToList();
    }

    public Task<WebhookHandlerResult> HandleAsync(string payload, string eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookPayload = JsonSerializer.Deserialize<SonarrWebhookPayload>(payload, JsonOptions);
            if (webhookPayload?.Series is null)
            {
                return Task.FromResult(WebhookHandlerResult.Succeeded(eventType: eventType));
            }

            var mediaItem = new MediaItem
            {
                Title = webhookPayload.Series.Title,
                Year = webhookPayload.Series.Year,
                Type = MediaType.Series
            };

            if (webhookPayload.Series.TvdbId > 0)
            {
                mediaItem.ExternalIds.Add(new ExternalId { Provider = "tvdb", Value = webhookPayload.Series.TvdbId.ToString() });
            }

            if (webhookPayload.Series.TmdbId > 0)
            {
                mediaItem.ExternalIds.Add(new ExternalId { Provider = "tmdb", Value = webhookPayload.Series.TmdbId.ToString() });
            }

            return Task.FromResult(WebhookHandlerResult.Succeeded(mediaItem, webhookPayload.EventType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle Sonarr webhook.");
            return Task.FromResult(WebhookHandlerResult.Failed(ex.Message));
        }
    }

    private static MediaItem MapToMediaItem(SonarrSeries series, Dictionary<int, string> tagMap)
    {
        var item = new MediaItem
        {
            Title = series.Title,
            Year = series.Year,
            Type = MediaType.Series
        };

        if (series.TvdbId > 0)
        {
            item.ExternalIds.Add(new ExternalId { Provider = "tvdb", Value = series.TvdbId.ToString() });
        }

        if (series.TmdbId > 0)
        {
            item.ExternalIds.Add(new ExternalId { Provider = "tmdb", Value = series.TmdbId.ToString() });
        }

        item.Tags = series.Tags
            .Where(tagId => tagMap.ContainsKey(tagId))
            .Select(tagId => new Tag { Name = tagMap[tagId] })
            .ToList();

        item.Genres = series.Genres;
        item.Artwork = series.Images
            .Where(image => !string.IsNullOrWhiteSpace(image.RemoteUrl))
            .Select(image => new Artwork
            {
                Type = image.CoverType.ToLowerInvariant() switch
                {
                    "poster" => ArtworkType.Poster,
                    "fanart" => ArtworkType.Fanart,
                    "banner" => ArtworkType.Banner,
                    _ => ArtworkType.Unknown
                },
                Url = image.RemoteUrl!
            })
            .ToList();

        return item;
    }
}
