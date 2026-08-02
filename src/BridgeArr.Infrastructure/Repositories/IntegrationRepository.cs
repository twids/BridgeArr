using BridgeArr.Application.Interfaces;
using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BridgeArr.Infrastructure.Repositories;

public class IntegrationRepository : IIntegrationRepository
{
    private readonly ApplicationDbContext _db;

    public IntegrationRepository(ApplicationDbContext db) => _db = db;

    public async Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Integrations.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Integration>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Integrations.OrderBy(i => i.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Integration>> GetEnabledAsync(CancellationToken cancellationToken = default)
        => await _db.Integrations.Where(i => i.Enabled).OrderBy(i => i.Name).ToListAsync(cancellationToken);

    public async Task<Integration> AddAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        _db.Integrations.Add(integration);
        await _db.SaveChangesAsync(cancellationToken);
        return integration;
    }

    public async Task<Integration> UpdateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        _db.Integrations.Update(integration);
        await _db.SaveChangesAsync(cancellationToken);
        return integration;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var integration = await _db.Integrations.FindAsync([id], cancellationToken);
        if (integration is not null)
        {
            _db.Integrations.Remove(integration);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
