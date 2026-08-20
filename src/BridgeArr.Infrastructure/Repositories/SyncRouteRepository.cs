using BridgeArr.Application.Interfaces;
using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BridgeArr.Infrastructure.Repositories;

public class SyncRouteRepository : ISyncRouteRepository
{
    private readonly ApplicationDbContext _db;
    public SyncRouteRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SyncRoute>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Query().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SyncRoute>> GetEnabledAsync(CancellationToken cancellationToken = default) =>
        await Query().Where(x => x.Enabled).OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<SyncRoute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<SyncRoute> AddAsync(SyncRoute route, CancellationToken cancellationToken = default)
    {
        _db.SyncRoutes.Add(route);
        await _db.SaveChangesAsync(cancellationToken);
        return route;
    }

    public async Task<SyncRoute> UpdateAsync(SyncRoute route, CancellationToken cancellationToken = default)
    {
        var existing = await _db.SyncRoutes.FindAsync([route.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Sync route '{route.Id}' was not found.");

        existing.Name = route.Name;
        existing.SourceIntegrationId = route.SourceIntegrationId;
        existing.TargetIntegrationId = route.TargetIntegrationId;
        existing.Enabled = route.Enabled;
        existing.IntervalMinutes = route.IntervalMinutes;
        existing.LastQueuedAt = route.LastQueuedAt;
        existing.CreatedAt = route.CreatedAt;
        existing.UpdatedAt = route.UpdatedAt;
        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var route = await _db.SyncRoutes.FindAsync([id], cancellationToken);
        if (route is null) return;
        _db.SyncRoutes.Remove(route);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<SyncRoute> Query() => _db.SyncRoutes
        .Include(x => x.SourceIntegration)
        .Include(x => x.TargetIntegration);
}
