using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Sonarr;
using Microsoft.Extensions.Logging.Abstractions;

namespace BridgeArr.UnitTests.Plugins;

public class SonarrPluginTests
{
    [Fact]
    public async Task HandleAsync_Maps_Webhook_Payload_To_MediaItem()
    {
        var plugin = new SonarrPlugin(new DummyHttpClientFactory(), NullLogger<SonarrPlugin>.Instance);
        var payload = """
            {
              "eventType": "Download",
              "series": {
                "id": 7,
                "title": "Breaking Bad",
                "year": 2008,
                "tvdbId": 81189,
                "tmdbId": 1396
              }
            }
            """;

        var result = await plugin.HandleAsync(payload, "Download");

        Assert.True(result.Success);
        Assert.NotNull(result.MediaItem);
        Assert.Equal("Breaking Bad", result.MediaItem!.Title);
        Assert.Equal(MediaType.Series, result.MediaItem.Type);
        Assert.Contains(result.MediaItem.ExternalIds, id => id.Provider == "tvdb" && id.Value == "81189");
        Assert.Contains(result.MediaItem.ExternalIds, id => id.Provider == "tmdb" && id.Value == "1396");
    }

    [Fact]
    public async Task HandleAsync_Returns_Success_When_Series_Is_Null()
    {
        var plugin = new SonarrPlugin(new DummyHttpClientFactory(), NullLogger<SonarrPlugin>.Instance);
        var payload = """{ "eventType": "Test" }""";

        var result = await plugin.HandleAsync(payload, "Test");

        Assert.True(result.Success);
        Assert.Null(result.MediaItem);
    }

    [Fact]
    public async Task HandleAsync_Returns_Failure_On_Invalid_Json()
    {
        var plugin = new SonarrPlugin(new DummyHttpClientFactory(), NullLogger<SonarrPlugin>.Instance);

        var result = await plugin.HandleAsync("not-valid-json", "Download");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    private sealed class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
