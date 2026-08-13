using BridgeArr.Application.Interfaces;
using BridgeArr.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Infrastructure.BackgroundServices;

/// <summary>Queues user-configured synchronization routes when they become due.</summary>
public class ScheduledSyncWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledSyncWorker> _logger;

    public ScheduledSyncWorker(IServiceProvider serviceProvider, ILogger<ScheduledSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled sync worker started.");
        await QueueDueRoutesAsync(stoppingToken);
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken)) await QueueDueRoutesAsync(stoppingToken);
    }

    public async Task QueueDueRoutesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISyncRouteRepository>();
        var service = scope.ServiceProvider.GetRequiredService<SyncRouteService>();
        var routes = await repository.GetEnabledAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var route in routes.Where(x => x.LastQueuedAt is null || x.LastQueuedAt.Value.AddMinutes(x.IntervalMinutes) <= now))
            await service.QueueAsync(route, cancellationToken: cancellationToken);
    }
}

