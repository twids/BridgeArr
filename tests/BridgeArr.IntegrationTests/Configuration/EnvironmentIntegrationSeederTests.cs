using System.Text.Json;
using BridgeArr.Infrastructure.Data;
using BridgeArr.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

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

    [Fact]
    public async Task SeedAsync_ExistingIntegration_UpdatesWithoutCreatingDuplicate()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var initialConfiguration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RADARR_URL"] = "http://radarr-old:7878",
            ["RADARR_APIKEY"] = "old-secret"
        });
        var updatedConfiguration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RADARR_URL"] = "http://radarr-new:7878",
            ["RADARR_APIKEY"] = "new-secret"
        });

        await EnvironmentIntegrationSeeder.SeedAsync(db, initialConfiguration, NullLogger.Instance);
        var originalId = (await db.Integrations.SingleAsync()).Id;
        await EnvironmentIntegrationSeeder.SeedAsync(db, updatedConfiguration, NullLogger.Instance);

        var integration = await db.Integrations.SingleAsync();
        Assert.Equal(originalId, integration.Id);
        using var json = JsonDocument.Parse(integration.ConfigurationJson);
        Assert.Equal("http://radarr-new:7878", json.RootElement.GetProperty("url").GetString());
        Assert.Equal("new-secret", json.RootElement.GetProperty("apiKey").GetString());
    }

    [Theory]
    [InlineData("radarr")]
    [InlineData("ftp://radarr:7878")]
    public void GetDefinitions_InvalidUrl_SkipsIntegration(string url)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RADARR_URL"] = url,
            ["RADARR_APIKEY"] = "secret"
        });

        Assert.Empty(EnvironmentIntegrationSeeder.GetDefinitions(configuration));
    }
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
