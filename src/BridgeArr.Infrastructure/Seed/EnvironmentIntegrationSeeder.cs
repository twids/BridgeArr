using System.Text.Json;
using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Infrastructure.Seed;

/// <summary>Creates or updates integrations configured through environment variables.</summary>
public static class EnvironmentIntegrationSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var definitions = GetDefinitions(configuration);

        foreach (var definition in definitions)
        {
            var integration = await db.Integrations
                .SingleOrDefaultAsync(x => x.PluginType == definition.PluginType, cancellationToken);

            if (integration is null)
            {
                integration = new Integration
                {
                    Name = definition.Name,
                    PluginType = definition.PluginType,
                    Enabled = true,
                    ConfigurationJson = definition.ConfigurationJson
                };
                db.Integrations.Add(integration);
                logger.LogInformation("Created {PluginType} integration from environment configuration.", definition.PluginType);
            }
            else
            {
                integration.Name = definition.Name;
                integration.Enabled = true;
                integration.ConfigurationJson = definition.ConfigurationJson;
                integration.UpdatedAt = DateTimeOffset.UtcNow;
                logger.LogInformation("Updated {PluginType} integration from environment configuration.", definition.PluginType);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static IReadOnlyList<EnvironmentIntegrationDefinition> GetDefinitions(IConfiguration configuration)
    {
        var definitions = new List<EnvironmentIntegrationDefinition>();
        AddIfComplete(definitions, "radarr", "Radarr", configuration["RADARR_URL"], "apiKey", configuration["RADARR_APIKEY"]);
        AddIfComplete(definitions, "plex", "Plex", configuration["PLEX_URL"], "token", configuration["PLEX_TOKEN"]);
        return definitions;
    }

    private static void AddIfComplete(List<EnvironmentIntegrationDefinition> definitions, string pluginType, string name, string? url, string secretName, string? secret)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(secret)) return;
        definitions.Add(new(pluginType, name, JsonSerializer.Serialize(new Dictionary<string, string> { ["url"] = url, [secretName] = secret })));
    }
}

public sealed record EnvironmentIntegrationDefinition(string PluginType, string Name, string ConfigurationJson);
