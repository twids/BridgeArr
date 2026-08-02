using System.Text.Json.Serialization;

namespace BridgeArr.Plugins.Sonarr.Models;

internal class SonarrWebhookPayload
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("series")]
    public SonarrWebhookSeries? Series { get; set; }
}

internal class SonarrWebhookSeries
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("tvdbId")]
    public int TvdbId { get; set; }

    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }
}
