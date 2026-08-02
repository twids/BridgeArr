using System.Text.Json.Serialization;

namespace BridgeArr.Plugins.Radarr.Models;

internal class RadarrWebhookPayload
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("movie")]
    public RadarrWebhookMovie? Movie { get; set; }
}

internal class RadarrWebhookMovie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    [JsonPropertyName("imdbId")]
    public string? ImdbId { get; set; }
}
