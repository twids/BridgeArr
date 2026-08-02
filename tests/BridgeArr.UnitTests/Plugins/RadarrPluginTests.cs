using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Radarr;
using Microsoft.Extensions.Logging.Abstractions;

namespace BridgeArr.UnitTests.Plugins;

public class RadarrPluginTests
{
    [Fact]
    public async Task HandleAsync_Maps_Webhook_Payload_To_MediaItem()
    {
        var plugin = new RadarrPlugin(new DummyHttpClientFactory(), NullLogger<RadarrPlugin>.Instance);
        var payload = """
            {
              "eventType": "Download",
              "movie": {
                "id": 42,
                "title": "Inception",
                "year": 2010,
                "tmdbId": 27205,
                "imdbId": "tt1375666"
              }
            }
            """;

        var result = await plugin.HandleAsync(payload, "Download");

        Assert.True(result.Success);
        Assert.NotNull(result.MediaItem);
        Assert.Equal("Inception", result.MediaItem!.Title);
        Assert.Equal(MediaType.Movie, result.MediaItem.Type);
        Assert.Contains(result.MediaItem.ExternalIds, id => id.Provider == "tmdb" && id.Value == "27205");
        Assert.Contains(result.MediaItem.ExternalIds, id => id.Provider == "imdb" && id.Value == "tt1375666");
    }

    private sealed class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
