using BridgeArr.Application.Interfaces;
using BridgeArr.Application.Diagnostics;
using BridgeArr.Domain.Entities;
using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Application.Services;

/// <summary>
/// Manages integration configurations.
/// </summary>
public class IntegrationService
{
    private readonly IIntegrationRepository _repository;
    private readonly IEnumerable<IPlugin> _plugins;
    private readonly ILogger<IntegrationService> _logger;

    public IntegrationService(
        IIntegrationRepository repository,
        IEnumerable<IPlugin> plugins,
        ILogger<IntegrationService> logger)
    {
        _repository = repository;
        _plugins = plugins;
        _logger = logger;
    }

    public Task<IReadOnlyList<Integration>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public async Task<Integration> CreateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        integration.CreatedAt = DateTimeOffset.UtcNow;
        integration.UpdatedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation(
            "Creating integration {Name} for plugin {PluginType}",
            LogSanitizer.Sanitize(integration.Name),
            LogSanitizer.Sanitize(integration.PluginType));
        return await _repository.AddAsync(integration, cancellationToken);
    }

    public async Task<Integration> UpdateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        integration.UpdatedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("Updating integration {IntegrationId}", integration.Id);
        return await _repository.UpdateAsync(integration, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting integration {IntegrationId}", id);
        return _repository.DeleteAsync(id, cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(Guid integrationId, CancellationToken cancellationToken = default)
    {
        var integration = await _repository.GetByIdAsync(integrationId, cancellationToken);
        if (integration is null)
        {
            return false;
        }

        var source = _plugins.OfType<IMediaSource>().FirstOrDefault(s => s.PluginType == integration.PluginType);
        if (source is not null)
        {
            return await source.TestConnectionAsync(integration.ConfigurationJson, cancellationToken);
        }

        var target = _plugins.OfType<IMediaTarget>().FirstOrDefault(t => t.PluginType == integration.PluginType);
        return target is not null && await target.TestConnectionAsync(integration.ConfigurationJson, cancellationToken);
    }

    public IEnumerable<IPlugin> GetAvailablePlugins() => _plugins;
}
