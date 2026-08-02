using BridgeArr.Domain.Entities;

namespace BridgeArr.Application.Interfaces;

/// <summary>Repository for ApplicationSetting entities.</summary>
public interface IApplicationSettingRepository
{
    Task<ApplicationSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationSetting> UpsertAsync(ApplicationSetting setting, CancellationToken cancellationToken = default);
}
