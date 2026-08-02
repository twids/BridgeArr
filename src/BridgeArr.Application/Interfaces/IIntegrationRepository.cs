using BridgeArr.Domain.Entities;

namespace BridgeArr.Application.Interfaces;

/// <summary>Repository for Integration entities.</summary>
public interface IIntegrationRepository
{
    Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Integration>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Integration>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<Integration> AddAsync(Integration integration, CancellationToken cancellationToken = default);
    Task<Integration> UpdateAsync(Integration integration, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
