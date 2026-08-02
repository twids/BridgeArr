using BridgeArr.Application.Interfaces;
using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Application.Services;

/// <summary>
/// Orchestrates media synchronization between source and target integrations.
/// </summary>
public class SyncService
{
    private readonly IIntegrationRepository _integrationRepository;
    private readonly ISyncJobRepository _syncJobRepository;
    private readonly ISyncQueue _syncQueue;
    private readonly IEnumerable<IMediaSource> _mediaSources;
    private readonly IEnumerable<IMediaTarget> _mediaTargets;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        IIntegrationRepository integrationRepository,
        ISyncJobRepository syncJobRepository,
        ISyncQueue syncQueue,
        IEnumerable<IMediaSource> mediaSources,
        IEnumerable<IMediaTarget> mediaTargets,
        ILogger<SyncService> logger)
    {
        _integrationRepository = integrationRepository;
        _syncJobRepository = syncJobRepository;
        _syncQueue = syncQueue;
        _mediaSources = mediaSources;
        _mediaTargets = mediaTargets;
        _logger = logger;
    }

    /// <summary>
    /// Queues a synchronization job for background processing.
    /// </summary>
    public async Task<SyncJob> RequestSyncAsync(
        Guid sourceIntegrationId,
        Guid targetIntegrationId,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        var job = new SyncJob
        {
            SourceIntegrationId = sourceIntegrationId,
            TargetIntegrationId = targetIntegrationId,
            Status = SyncJobStatus.Queued,
            Payload = payload
        };

        job = await _syncJobRepository.AddAsync(job, cancellationToken);
        await _syncQueue.EnqueueAsync(job, cancellationToken);

        _logger.LogInformation(
            "Sync job {JobId} queued for source {SourceId} -> target {TargetId}",
            job.Id,
            sourceIntegrationId,
            targetIntegrationId);

        return job;
    }

    /// <summary>
    /// Executes a synchronization job.
    /// </summary>
    public async Task ExecuteSyncAsync(SyncJob job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing sync job {JobId}", job.Id);

        job.Status = SyncJobStatus.Running;
        job.StartedAt = DateTimeOffset.UtcNow;
        await _syncJobRepository.UpdateAsync(job, cancellationToken);

        try
        {
            var sourceIntegration = await _integrationRepository.GetByIdAsync(job.SourceIntegrationId, cancellationToken);
            var targetIntegration = await _integrationRepository.GetByIdAsync(job.TargetIntegrationId, cancellationToken);

            if (sourceIntegration is null || targetIntegration is null)
            {
                throw new InvalidOperationException("Source or target integration not found.");
            }

            var source = _mediaSources.FirstOrDefault(s => s.PluginType == sourceIntegration.PluginType);
            var target = _mediaTargets.FirstOrDefault(t => t.PluginType == targetIntegration.PluginType);

            if (source is null)
            {
                throw new InvalidOperationException($"Media source plugin '{sourceIntegration.PluginType}' not found.");
            }

            if (target is null)
            {
                throw new InvalidOperationException($"Media target plugin '{targetIntegration.PluginType}' not found.");
            }

            var mediaItems = await source.GetAllMediaAsync(sourceIntegration.ConfigurationJson, cancellationToken);
            _logger.LogInformation("Retrieved {Count} items from source {Source}", mediaItems.Count, source.PluginType);

            var synced = 0;
            foreach (var item in mediaItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var targetId = await target.FindMediaAsync(item, targetIntegration.ConfigurationJson, cancellationToken);
                if (targetId is null)
                {
                    _logger.LogDebug("Media item '{Title}' not found in target, skipping.", item.Title);
                    continue;
                }

                var labels = item.Tags.Select(t => t.Name).ToList();
                var updated = await target.SetLabelsAsync(targetId, labels, targetIntegration.ConfigurationJson, cancellationToken);
                if (updated)
                {
                    synced++;
                }
            }

            job.Status = SyncJobStatus.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("Sync job {JobId} completed. Synced {Count} items.", job.Id, synced);
        }
        catch (Exception ex)
        {
            job.Status = SyncJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.RetryCount++;
            _logger.LogError(ex, "Sync job {JobId} failed.", job.Id);
        }

        await _syncJobRepository.UpdateAsync(job, cancellationToken);
    }
}
