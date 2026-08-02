using BridgeArr.Domain.Entities;

namespace BridgeArr.Application.Interfaces;

/// <summary>
/// Queue for synchronization jobs - enables immediate webhook return.
/// </summary>
public interface ISyncQueue
{
    /// <summary>Enqueues a sync job for background processing.</summary>
    ValueTask EnqueueAsync(SyncJob job, CancellationToken cancellationToken = default);

    /// <summary>Dequeues the next pending sync job.</summary>
    ValueTask<SyncJob?> DequeueAsync(CancellationToken cancellationToken = default);
}
