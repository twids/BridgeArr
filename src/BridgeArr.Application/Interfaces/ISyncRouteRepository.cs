using BridgeArr.Domain.Entities;

namespace BridgeArr.Application.Interfaces;

/// <summary>Repository for user-configured synchronization routes.</summary>
public interface ISyncRouteRepository
{
    Task<IReadOnlyList<SyncRoute>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SyncRoute>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<SyncRoute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SyncRoute> AddAsync(SyncRoute route, CancellationToken cancellationToken = default);
    Task<SyncRoute> UpdateAsync(SyncRoute route, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
