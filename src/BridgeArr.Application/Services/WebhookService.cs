using BridgeArr.Application.Diagnostics;
using BridgeArr.Application.Interfaces;
using BridgeArr.Domain.Entities;
using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Application.Services;

/// <summary>Handles incoming webhook events from external systems.</summary>
public class WebhookService
{
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly ISyncRouteRepository _syncRouteRepository;
    private readonly SyncRouteService _syncRouteService;
    private readonly IEnumerable<IWebhookHandler> _handlers;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IWebhookEventRepository webhookEventRepository,
        IIntegrationRepository integrationRepository,
        ISyncRouteRepository syncRouteRepository,
        SyncRouteService syncRouteService,
        IEnumerable<IWebhookHandler> handlers,
        ILogger<WebhookService> logger)
    {
        _webhookEventRepository = webhookEventRepository;
        _integrationRepository = integrationRepository;
        _syncRouteRepository = syncRouteRepository;
        _syncRouteService = syncRouteService;
        _handlers = handlers;
        _logger = logger;
    }

    public async Task<WebhookEvent> ReceiveAsync(
        string source,
        string eventType,
        string payload,
        CancellationToken cancellationToken = default)
    {
        var webhookEvent = new WebhookEvent
        {
            Source = source,
            EventType = eventType,
            Payload = payload,
            Processed = false
        };

        webhookEvent = await _webhookEventRepository.AddAsync(webhookEvent, cancellationToken);
        _logger.LogInformation(
            "Received webhook from {Source} type {EventType}, event ID {EventId}",
            LogSanitizer.Sanitize(source),
            LogSanitizer.Sanitize(eventType),
            webhookEvent.Id);
        return webhookEvent;
    }

    public async Task ProcessAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var handler = _handlers.FirstOrDefault(h => h.Source == webhookEvent.Source);
        if (handler is null)
        {
            _logger.LogWarning("No handler found for webhook source {Source}", LogSanitizer.Sanitize(webhookEvent.Source));
            webhookEvent.Processed = true;
            webhookEvent.ProcessingError = $"No handler registered for source: {webhookEvent.Source}";
            webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
            await _webhookEventRepository.UpdateAsync(webhookEvent, cancellationToken);
            return;
        }

        try
        {
            var result = await handler.HandleAsync(webhookEvent.Payload, webhookEvent.EventType, cancellationToken);
            if (result.Success && result.MediaItem is not null)
            {
                var integrations = await _integrationRepository.GetEnabledAsync(cancellationToken);
                var sourceIntegration = integrations.FirstOrDefault(i => i.PluginType.Equals(handler.PluginType, StringComparison.OrdinalIgnoreCase));
                if (sourceIntegration is not null)
                {
                    var routes = await _syncRouteRepository.GetEnabledAsync(cancellationToken);
                    foreach (var route in routes.Where(x => x.SourceIntegrationId == sourceIntegration.Id))
                        await _syncRouteService.QueueAsync(route, webhookEvent.Payload, cancellationToken);
                }
            }

            webhookEvent.Processed = true;
            webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
            if (!result.Success) webhookEvent.ProcessingError = result.ErrorMessage;
        }
        catch (Exception ex)
        {
            webhookEvent.Processed = true;
            webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
            webhookEvent.ProcessingError = ex.Message;
            _logger.LogError(ex, "Error processing webhook event {EventId}", webhookEvent.Id);
        }

        await _webhookEventRepository.UpdateAsync(webhookEvent, cancellationToken);
    }
}
