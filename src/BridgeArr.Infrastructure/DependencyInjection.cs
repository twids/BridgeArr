using BridgeArr.Application.Interfaces;
using BridgeArr.Infrastructure.BackgroundServices;
using BridgeArr.Infrastructure.Data;
using BridgeArr.Infrastructure.Queue;
using BridgeArr.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeArr.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<ISyncJobRepository, SyncJobRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
        services.AddScoped<IApplicationSettingRepository, ApplicationSettingRepository>();

        services.AddSingleton<ISyncQueue, InMemorySyncQueue>();
        services.AddHostedService<SyncWorker>();
        services.AddHostedService<ScheduledSyncWorker>();

        return services;
    }
}
