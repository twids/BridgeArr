using BridgeArr.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeArr.Application;

/// <summary>
/// Extension methods for registering application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all application layer services.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SyncService>();
        services.AddScoped<WebhookService>();
        services.AddScoped<SyncRouteService>();
        services.AddScoped<IntegrationService>();
        return services;
    }
}
