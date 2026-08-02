using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeArr.Plugins.Sonarr;

/// <summary>
/// Extension methods for registering Sonarr plugin services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSonarrPlugin(this IServiceCollection services)
    {
        services.AddHttpClient("sonarr");
        services.AddSingleton<SonarrPlugin>();
        services.AddSingleton<IPlugin>(sp => sp.GetRequiredService<SonarrPlugin>());
        services.AddSingleton<IMediaSource>(sp => sp.GetRequiredService<SonarrPlugin>());
        services.AddSingleton<IWebhookHandler>(sp => sp.GetRequiredService<SonarrPlugin>());
        return services;
    }
}
