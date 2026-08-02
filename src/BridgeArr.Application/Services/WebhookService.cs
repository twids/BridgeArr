using BridgeArr.Application.Interfaces;
using BridgeArr.Application.Diagnostics;
using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Application.Services;

/// <summary>
/// Handles incoming webhook events from external systems.
/// Persists the event and enqueues processing; returns immediately.
/// </summary>
public class WebhookService
{
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly ISyncQueue _syncQueue;
    private readonly ISyncJobRepository _syncJobRepository;
    private readonly IEnumerable<IWebhookHandler> _handlers;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IWebhookEventRepository webhookEventRepository,
        IIntegrationRepository integrationRepository,
        ISyncQueue syncQueue,
        ISyncJobRepository syncJobRepository,
        IEnumerable<IWebhookHandler> handlers,
        ILogger<WebhookService> logger)
    {
        _webhookEventRepository = webhookEventRepository;
        _integrationRepository = integrationRepository;
        _syncQueue = syncQueue;
        _syncJobRepository = syncJobRepository;
        _handlers = handlers;
        _logger = logger;
    }

    /// <summary>
    /// Receives a webhook payload, persists it, and enqueues for processing.
    /// Returns immediately without blocking.
    /// </summary>
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
        var sanitizedSource = LogSanitizer.Sanitize(source);
        var sanitizedEventType = LogSanitizer.Sanitize(eventType);
        _logger.LogInformation(
            "Received webhook from {Source} type {EventType}, event ID {EventId}",
            sanitizedSource,
            sanitizedEventType,
            webhookEvent.Id);

        return webhookEvent;
    }

    /// <summary>
    /// Processes a persisted webhook event.
    /// </summary>
    public async Task ProcessAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var handler = _handlers.FirstOrDefault(h => h.Source == webhookEvent.Source);
        if (handler is null)
        {
            _logger.LogWarning(
                "No handler found for webhook source {Source}",
                LogSanitizer.Sanitize(webhookEvent.Source));
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
                var sourceIntegration = integrations.FirstOrDefault(i => i.PluginType == handler.PluginType);
                var targetIntegrations = integrations.Where(i => i.PluginType != handler.PluginType).ToList();

                if (sourceIntegration is not null)
                {
                    foreach (var target in targetIntegrations)
                    {
                        var job = new SyncJob
                        {
                            SourceIntegrationId = sourceIntegration.Id,
                            TargetIntegrationId = target.Id,
                            Status = SyncJobStatus.Queued,
                            Payload = webhookEvent.Payload
                        };

                        job = await _syncJobRepository.AddAsync(job, cancellationToken);
                        await _syncQueue.EnqueueAsync(job, cancellationToken);
                    }
                }
            }

            webhookEvent.Processed = true;
            webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
            if (!result.Success)
            {
                webhookEvent.ProcessingError = result.ErrorMessage;
            }
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
