using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.Data;
using BridgeArr.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BridgeArr.IntegrationTests.Repositories;

public class WebhookEventRepositoryTests
{
    [Fact]
    public async Task GetRecentAsync_MixedStatuses_ReturnsNewestEventsIncludingProcessed()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var oldest = new WebhookEvent { Source = "radarr", EventType = "Old", ReceivedAt = DateTimeOffset.UtcNow.AddMinutes(-3) };
        var processed = new WebhookEvent { Source = "sonarr", EventType = "Processed", Processed = true, ProcessedAt = DateTimeOffset.UtcNow, ReceivedAt = DateTimeOffset.UtcNow.AddMinutes(-2) };
        var failed = new WebhookEvent { Source = "radarr", EventType = "Failed", Processed = true, ProcessingError = "No route", ReceivedAt = DateTimeOffset.UtcNow.AddMinutes(-1) };
        db.WebhookEvents.AddRange(oldest, processed, failed);
        await db.SaveChangesAsync();
        var repository = new WebhookEventRepository(db);

        var events = await repository.GetRecentAsync(2);

        Assert.Equal([failed.Id, processed.Id], events.Select(x => x.Id));
        Assert.Contains(events, x => x.Processed);
        Assert.Contains(events, x => x.ProcessingError is not null);
    }
}
