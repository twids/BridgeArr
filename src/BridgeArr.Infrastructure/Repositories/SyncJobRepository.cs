using BridgeArr.Application.Interfaces;
using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;
using BridgeArr.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BridgeArr.Infrastructure.Repositories;

public class SyncJobRepository : ISyncJobRepository
{
    private readonly ApplicationDbContext _db;

    public SyncJobRepository(ApplicationDbContext db) => _db = db;

    public async Task<SyncJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.SyncJobs
            .Include(j => j.SourceIntegration)
            .Include(j => j.TargetIntegration)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SyncJob>> GetPendingAsync(CancellationToken cancellationToken = default)
        => await _db.SyncJobs
            .Where(j => j.Status == SyncJobStatus.Queued)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SyncJob>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
        => await _db.SyncJobs
            .Include(j => j.SourceIntegration)
            .Include(j => j.TargetIntegration)
            .OrderByDescending(j => j.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<SyncJob> AddAsync(SyncJob job, CancellationToken cancellationToken = default)
    {
        _db.SyncJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<SyncJob> UpdateAsync(SyncJob job, CancellationToken cancellationToken = default)
    {
        _db.SyncJobs.Update(job);
        await _db.SaveChangesAsync(cancellationToken);
        return job;
    }
}
