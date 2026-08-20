using BridgeArr.Application.Interfaces;
using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Application.Services;

/// <summary>Manages and runs user-configured synchronization routes.</summary>
public class SyncRouteService
{
    private readonly ISyncRouteRepository _routes;
    private readonly IIntegrationRepository _integrations;
    private readonly ISyncJobRepository _jobs;
    private readonly SyncService _syncService;
    private readonly IEnumerable<IPlugin> _plugins;
    private readonly ILogger<SyncRouteService> _logger;

    public SyncRouteService(ISyncRouteRepository routes, IIntegrationRepository integrations, ISyncJobRepository jobs,
        SyncService syncService, IEnumerable<IPlugin> plugins, ILogger<SyncRouteService> logger)
    {
        _routes = routes;
        _integrations = integrations;
        _jobs = jobs;
        _syncService = syncService;
        _plugins = plugins;
        _logger = logger;
    }

    public Task<IReadOnlyList<SyncRoute>> GetAllAsync(CancellationToken cancellationToken = default) => _routes.GetAllAsync(cancellationToken);

    public async Task<SyncRoute> CreateAsync(SyncRoute route, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(route, cancellationToken);
        route.Id = Guid.NewGuid();
        route.CreatedAt = route.UpdatedAt = DateTimeOffset.UtcNow;
        return await _routes.AddAsync(route, cancellationToken);
    }

    public async Task<SyncRoute> UpdateAsync(SyncRoute route, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(route, cancellationToken);
        route.UpdatedAt = DateTimeOffset.UtcNow;
        return await _routes.UpdateAsync(route, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => _routes.DeleteAsync(id, cancellationToken);

    public async Task<SyncJob?> QueueAsync(SyncRoute route, string? payload = null, CancellationToken cancellationToken = default)
    {
        var jobs = await _jobs.GetRecentAsync(100, cancellationToken);
        if (jobs.Any(x => x.SourceIntegrationId == route.SourceIntegrationId && x.TargetIntegrationId == route.TargetIntegrationId &&
            (x.Status == SyncJobStatus.Queued || x.Status == SyncJobStatus.Running)))
        {
            _logger.LogInformation("Sync route {RouteId} skipped because a matching job is active.", route.Id);
            return null;
        }

        var job = await _syncService.RequestSyncAsync(route.SourceIntegrationId, route.TargetIntegrationId, payload, cancellationToken);
        route.LastQueuedAt = DateTimeOffset.UtcNow;
        route.UpdatedAt = route.LastQueuedAt.Value;
        await _routes.UpdateAsync(route, cancellationToken);
        return job;
    }

    private async Task ValidateAsync(SyncRoute route, CancellationToken cancellationToken)
    {
        if (route.SourceIntegrationId == route.TargetIntegrationId) throw new InvalidOperationException("Source and target must differ.");
        if (route.IntervalMinutes < 1) throw new InvalidOperationException("Interval must be at least one minute.");
        var source = await _integrations.GetByIdAsync(route.SourceIntegrationId, cancellationToken) ?? throw new InvalidOperationException("Source integration not found.");
        var target = await _integrations.GetByIdAsync(route.TargetIntegrationId, cancellationToken) ?? throw new InvalidOperationException("Target integration not found.");
        if (!_plugins.OfType<IMediaSource>().Any(x => x.PluginType.Equals(source.PluginType, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Selected integration is not a media source.");
        if (!_plugins.OfType<IMediaTarget>().Any(x => x.PluginType.Equals(target.PluginType, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Selected integration is not a media target.");
    }
}

