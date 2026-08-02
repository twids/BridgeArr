using BridgeArr.Application.Interfaces;
using BridgeArr.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Infrastructure.BackgroundServices;

/// <summary>
/// Background worker that processes the sync job queue.
/// </summary>
public class SyncWorker : BackgroundService
{
    private readonly ISyncQueue _syncQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncWorker> _logger;

    public SyncWorker(ISyncQueue syncQueue, IServiceProvider serviceProvider, ILogger<SyncWorker> logger)
    {
        _syncQueue = syncQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sync worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await _syncQueue.DequeueAsync(stoppingToken);
            if (job is null)
            {
                continue;
            }

            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();

            try
            {
                await syncService.ExecuteSyncAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing sync job {JobId}", job.Id);
            }
        }

        _logger.LogInformation("Sync worker stopped.");
    }
}
