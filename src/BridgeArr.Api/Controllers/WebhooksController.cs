using BridgeArr.Application.Services;
using BridgeArr.Application.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BridgeArr.Api.Controllers;

/// <summary>
/// Receives incoming webhooks from external systems.
/// Persists and processes webhook events before acknowledging them.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly WebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(WebhookService webhookService, ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpPost("{source}")]
    public async Task<IActionResult> Receive(
        string source,
        [FromHeader(Name = "X-Webhook-Event")] string? eventType,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        var webhookEvent = await _webhookService.ReceiveAsync(
            source.ToLowerInvariant(),
            eventType ?? "unknown",
            payload,
            cancellationToken);

        await _webhookService.ProcessAsync(webhookEvent, cancellationToken);
        _logger.LogInformation(
            "Accepted webhook {WebhookEventId} from {Source}",
            webhookEvent.Id,
            LogSanitizer.Sanitize(source));

        return Accepted(new { webhookEvent.Id });
    }
}
