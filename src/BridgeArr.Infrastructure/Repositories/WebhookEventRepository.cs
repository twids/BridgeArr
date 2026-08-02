using BridgeArr.Application.Interfaces;
using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BridgeArr.Infrastructure.Repositories;

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly ApplicationDbContext _db;

    public WebhookEventRepository(ApplicationDbContext db) => _db = db;

    public async Task<WebhookEvent> AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _db.WebhookEvents.Add(webhookEvent);
        await _db.SaveChangesAsync(cancellationToken);
        return webhookEvent;
    }

    public async Task<WebhookEvent> UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _db.WebhookEvents.Update(webhookEvent);
        await _db.SaveChangesAsync(cancellationToken);
        return webhookEvent;
    }

    public async Task<IReadOnlyList<WebhookEvent>> GetUnprocessedAsync(CancellationToken cancellationToken = default)
        => await _db.WebhookEvents
            .Where(e => !e.Processed)
            .OrderBy(e => e.ReceivedAt)
            .ToListAsync(cancellationToken);
}
