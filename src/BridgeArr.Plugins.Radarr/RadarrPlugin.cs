using System.Text.Json;
using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Abstractions;
using BridgeArr.Plugins.Radarr.Models;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Plugins.Radarr;

/// <summary>
/// Radarr plugin implementing IMediaSource and IWebhookHandler.
/// </summary>
public class RadarrPlugin : IMediaSource, IWebhookHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RadarrPlugin> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string PluginType => "radarr";
    public string DisplayName => "Radarr";
    public string Version => "1.0.0";
    public string Source => "radarr";
    public PluginCapabilities Capabilities => PluginCapabilities.MediaSource | PluginCapabilities.WebhookHandler;

    public RadarrPlugin(IHttpClientFactory httpClientFactory, ILogger<RadarrPlugin> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private RadarrApiClient CreateClient(string configurationJson)
    {
        var config = JsonSerializer.Deserialize<RadarrConfiguration>(configurationJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid Radarr configuration.");

        var httpClient = _httpClientFactory.CreateClient("radarr");
        httpClient.BaseAddress = new Uri(config.Url.TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
        return new RadarrApiClient(httpClient);
    }

    public async Task<bool> TestConnectionAsync(string configurationJson, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(configurationJson);
            return await client.TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Radarr connection test failed.");
            return false;
        }
    }

    public async Task<IReadOnlyList<MediaItem>> GetAllMediaAsync(string configurationJson, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(configurationJson);
        var movies = await client.GetMoviesAsync(cancellationToken);
        var tags = await client.GetTagsAsync(cancellationToken);
        var tagMap = tags.ToDictionary(t => t.Id, t => t.Label);

        return movies.Select(m => MapToMediaItem(m, tagMap)).ToList();
    }

    public async Task<MediaItem?> GetMediaByIdAsync(string sourceId, string configurationJson, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(sourceId, out var id))
        {
            return null;
        }

        var client = CreateClient(configurationJson);
        var movie = await client.GetMovieAsync(id, cancellationToken);
        if (movie is null)
        {
            return null;
        }

        var tags = await client.GetTagsAsync(cancellationToken);
        var tagMap = tags.ToDictionary(t => t.Id, t => t.Label);
        return MapToMediaItem(movie, tagMap);
    }

    public async Task<IReadOnlyList<Tag>> GetTagsAsync(string configurationJson, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(configurationJson);
        var radarrTags = await client.GetTagsAsync(cancellationToken);
        return radarrTags.Select(t => new Tag { Name = t.Label }).ToList();
    }

    public Task<WebhookHandlerResult> HandleAsync(string payload, string eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookPayload = JsonSerializer.Deserialize<RadarrWebhookPayload>(payload, JsonOptions);
            if (webhookPayload?.Movie is null)
            {
                return Task.FromResult(WebhookHandlerResult.Succeeded(eventType: eventType));
            }

            var mediaItem = new MediaItem
            {
                Title = webhookPayload.Movie.Title,
                Year = webhookPayload.Movie.Year,
                Type = MediaType.Movie
            };

            if (webhookPayload.Movie.TmdbId > 0)
            {
                mediaItem.ExternalIds.Add(new ExternalId { Provider = "tmdb", Value = webhookPayload.Movie.TmdbId.ToString() });
            }

            if (!string.IsNullOrWhiteSpace(webhookPayload.Movie.ImdbId))
            {
                mediaItem.ExternalIds.Add(new ExternalId { Provider = "imdb", Value = webhookPayload.Movie.ImdbId });
            }

            return Task.FromResult(WebhookHandlerResult.Succeeded(mediaItem, webhookPayload.EventType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle Radarr webhook.");
            return Task.FromResult(WebhookHandlerResult.Failed(ex.Message));
        }
    }

    private static MediaItem MapToMediaItem(RadarrMovie movie, Dictionary<int, string> tagMap)
    {
        var item = new MediaItem
        {
            Title = movie.Title,
            Year = movie.Year,
            Type = MediaType.Movie
        };

        if (movie.TmdbId > 0)
        {
            item.ExternalIds.Add(new ExternalId { Provider = "tmdb", Value = movie.TmdbId.ToString() });
        }

        if (!string.IsNullOrWhiteSpace(movie.ImdbId))
        {
            item.ExternalIds.Add(new ExternalId { Provider = "imdb", Value = movie.ImdbId });
        }

        item.Tags = movie.Tags
            .Where(tagId => tagMap.ContainsKey(tagId))
            .Select(tagId => new Tag { Name = tagMap[tagId] })
            .ToList();

        item.Genres = movie.Genres;
        item.Artwork = movie.Images
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
