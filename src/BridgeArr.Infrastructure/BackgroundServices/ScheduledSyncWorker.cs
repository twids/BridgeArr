using BridgeArr.Application.Interfaces;
using BridgeArr.Application.Services;
using BridgeArr.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Infrastructure.BackgroundServices;

/// <summary>Periodically queues full Radarr-to-Plex synchronization jobs.</summary>
public class ScheduledSyncWorker : BackgroundService
{
    public const int DefaultIntervalMinutes = 60;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledSyncWorker> _logger;
    private readonly TimeSpan _interval;

    public ScheduledSyncWorker(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<ScheduledSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = GetInterval(configuration);
    }

    public static TimeSpan GetInterval(IConfiguration configuration)
    {
        var value = configuration["SYNC_INTERVAL_MINUTES"];
        return int.TryParse(value, out var minutes) && minutes > 0
            ? TimeSpan.FromMinutes(minutes)
            : TimeSpan.FromMinutes(DefaultIntervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled sync worker started with interval {Interval}.", _interval);
        await QueueSyncIfNeededAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await QueueSyncIfNeededAsync(stoppingToken);
    }

    public async Task QueueSyncIfNeededAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var integrations = await scope.ServiceProvider.GetRequiredService<IIntegrationRepository>().GetEnabledAsync(cancellationToken);
        var source = integrations.FirstOrDefault(x => x.PluginType.Equals("radarr", StringComparison.OrdinalIgnoreCase));
        var target = integrations.FirstOrDefault(x => x.PluginType.Equals("plex", StringComparison.OrdinalIgnoreCase));
        if (source is null || target is null)
        {
            _logger.LogWarning("Scheduled sync skipped because enabled Radarr and Plex integrations are required.");
            return;
        }

        var jobs = await scope.ServiceProvider.GetRequiredService<ISyncJobRepository>().GetRecentAsync(100, cancellationToken);
        var alreadyActive = jobs.Any(x => x.SourceIntegrationId == source.Id && x.TargetIntegrationId == target.Id &&
            (x.Status == SyncJobStatus.Queued || x.Status == SyncJobStatus.Running));
        if (alreadyActive)
        {
            _logger.LogInformation("Scheduled sync skipped because a Radarr-to-Plex job is already active.");
            return;
        }

        await scope.ServiceProvider.GetRequiredService<SyncService>()
            .RequestSyncAsync(source.Id, target.Id, cancellationToken: cancellationToken);
    }
}
