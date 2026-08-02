using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeArr.Plugins.Plex;

/// <summary>
/// Extension methods for registering Plex plugin services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPlexPlugin(this IServiceCollection services)
    {
        services.AddHttpClient("plex");
        services.AddSingleton<PlexPlugin>();
        services.AddSingleton<IPlugin>(sp => sp.GetRequiredService<PlexPlugin>());
        services.AddSingleton<IMediaTarget>(sp => sp.GetRequiredService<PlexPlugin>());
        return services;
    }
}
