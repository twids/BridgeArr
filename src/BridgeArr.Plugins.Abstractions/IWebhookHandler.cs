using BridgeArr.Domain.Entities;

namespace BridgeArr.Plugins.Abstractions;

/// <summary>
/// Defines the contract for handling incoming webhooks from external systems.
/// </summary>
public interface IWebhookHandler : IPlugin
{
    /// <summary>
    /// Gets the source identifier this handler processes (e.g., "radarr", "sonarr").
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Parses and handles an incoming webhook payload.
    /// Returns a MediaItem if the webhook represents a media event, otherwise null.
    /// </summary>
    Task<WebhookHandlerResult> HandleAsync(string payload, string eventType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of processing a webhook event.
/// </summary>
public class WebhookHandlerResult
{
    /// <summary>Gets or sets whether the webhook was handled successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the parsed media item, if applicable.</summary>
    public MediaItem? MediaItem { get; set; }

    /// <summary>Gets or sets the event type that was handled.</summary>
    public string? HandledEventType { get; set; }

    /// <summary>Gets or sets an error message if handling failed.</summary>
    public string? ErrorMessage { get; set; }

    public static WebhookHandlerResult Succeeded(MediaItem? mediaItem = null, string? eventType = null) =>
        new() { Success = true, MediaItem = mediaItem, HandledEventType = eventType };

    public static WebhookHandlerResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
