using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeArr.Plugins.Radarr;

/// <summary>
/// Extension methods for registering Radarr plugin services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRadarrPlugin(this IServiceCollection services)
    {
        services.AddHttpClient("radarr");
        services.AddSingleton<RadarrPlugin>();
        services.AddSingleton<IPlugin>(sp => sp.GetRequiredService<RadarrPlugin>());
        services.AddSingleton<IMediaSource>(sp => sp.GetRequiredService<RadarrPlugin>());
        services.AddSingleton<IWebhookHandler>(sp => sp.GetRequiredService<RadarrPlugin>());
        return services;
    }
}
