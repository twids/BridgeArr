using BridgeArr.Application.Interfaces;
using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BridgeArr.Infrastructure.Repositories;

public class ApplicationSettingRepository : IApplicationSettingRepository
{
    private readonly ApplicationDbContext _db;

    public ApplicationSettingRepository(ApplicationDbContext db) => _db = db;

    public async Task<ApplicationSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        => await _db.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

    public async Task<IReadOnlyList<ApplicationSetting>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.ApplicationSettings.OrderBy(s => s.Key).ToListAsync(cancellationToken);

    public async Task<ApplicationSetting> UpsertAsync(ApplicationSetting setting, CancellationToken cancellationToken = default)
    {
        var existing = await _db.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == setting.Key, cancellationToken);
        if (existing is null)
        {
            _db.ApplicationSettings.Add(setting);
        }
        else
        {
            existing.Value = setting.Value;
            existing.Description = setting.Description;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return existing ?? setting;
    }
}
