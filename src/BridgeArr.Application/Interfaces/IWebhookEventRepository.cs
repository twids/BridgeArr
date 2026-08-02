using BridgeArr.Domain.Entities;

namespace BridgeArr.Application.Interfaces;

/// <summary>Repository for WebhookEvent entities.</summary>
public interface IWebhookEventRepository
{
    Task<WebhookEvent> AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task<WebhookEvent> UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookEvent>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
}
