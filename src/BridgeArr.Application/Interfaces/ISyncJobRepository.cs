using BridgeArr.Domain.Entities;

namespace BridgeArr.Application.Interfaces;

/// <summary>Repository for SyncJob entities.</summary>
public interface ISyncJobRepository
{
    Task<SyncJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SyncJob>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SyncJob>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);
    Task<SyncJob> AddAsync(SyncJob job, CancellationToken cancellationToken = default);
    Task<SyncJob> UpdateAsync(SyncJob job, CancellationToken cancellationToken = default);
}
