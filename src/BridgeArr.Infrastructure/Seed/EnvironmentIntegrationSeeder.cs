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
            var matchingIntegrations = await db.Integrations
                .Where(x => x.PluginType == definition.PluginType)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            var integration = matchingIntegrations.FirstOrDefault();

            if (matchingIntegrations.Count > 1)
                logger.LogWarning("Found multiple {PluginType} integrations; updating the oldest entry.", definition.PluginType);

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
        var normalizedUrl = url?.Trim();
        var normalizedSecret = secret?.Trim();
        if (string.IsNullOrEmpty(normalizedUrl) || string.IsNullOrEmpty(normalizedSecret) ||
            !Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var parsedUrl) ||
            (parsedUrl.Scheme != Uri.UriSchemeHttp && parsedUrl.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        definitions.Add(new(pluginType, name, JsonSerializer.Serialize(new Dictionary<string, string> { ["url"] = normalizedUrl, [secretName] = normalizedSecret })));
    }
}

public sealed record EnvironmentIntegrationDefinition(string PluginType, string Name, string ConfigurationJson);
