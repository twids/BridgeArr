using System.Text.Json;
using BridgeArr.Infrastructure.Seed;
using Microsoft.Extensions.Configuration;

namespace BridgeArr.IntegrationTests.Configuration;

public class EnvironmentIntegrationSeederTests
{
    [Fact]
    public void GetDefinitions_CompleteRadarrAndPlexConfiguration_ReturnsBothIntegrations()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RADARR_URL"] = "http://radarr:7878",
            ["RADARR_APIKEY"] = "radarr-secret",
            ["PLEX_URL"] = "http://plex:32400",
            ["PLEX_TOKEN"] = "plex-secret"
        });

        var definitions = EnvironmentIntegrationSeeder.GetDefinitions(configuration);

        Assert.Collection(
            definitions,
            radarr =>
            {
                Assert.Equal("radarr", radarr.PluginType);
                using var json = JsonDocument.Parse(radarr.ConfigurationJson);
                Assert.Equal("http://radarr:7878", json.RootElement.GetProperty("url").GetString());
                Assert.Equal("radarr-secret", json.RootElement.GetProperty("apiKey").GetString());
            },
            plex =>
            {
                Assert.Equal("plex", plex.PluginType);
                using var json = JsonDocument.Parse(plex.ConfigurationJson);
                Assert.Equal("http://plex:32400", json.RootElement.GetProperty("url").GetString());
                Assert.Equal("plex-secret", json.RootElement.GetProperty("token").GetString());
            });
    }

    [Fact]
    public void GetDefinitions_IncompleteConfiguration_SkipsIntegration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RADARR_URL"] = "http://radarr:7878",
            ["RADARR_APIKEY"] = ""
        });

        Assert.Empty(EnvironmentIntegrationSeeder.GetDefinitions(configuration));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
