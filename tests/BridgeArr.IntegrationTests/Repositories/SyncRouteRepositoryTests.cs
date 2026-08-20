using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.Data;
using BridgeArr.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BridgeArr.IntegrationTests.Repositories;

public class SyncRouteRepositoryTests
{
    [Fact]
    public async Task UpdateAsync_TrackedRouteAndDetachedChanges_UpdatesExistingRoute()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var route = new SyncRoute
        {
            Name = "Original",
            SourceIntegrationId = Guid.NewGuid(),
            TargetIntegrationId = Guid.NewGuid(),
            IntervalMinutes = 60
        };
        db.SyncRoutes.Add(route);
        await db.SaveChangesAsync();
        var repository = new SyncRouteRepository(db);
        await repository.GetAllAsync();
        var changed = new SyncRoute
        {
            Id = route.Id,
            Name = "Changed",
            SourceIntegrationId = route.SourceIntegrationId,
            TargetIntegrationId = route.TargetIntegrationId,
            IntervalMinutes = 15,
            Enabled = false,
            CreatedAt = route.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var updated = await repository.UpdateAsync(changed);

        Assert.Same(route, updated);
        Assert.Equal("Changed", route.Name);
        Assert.Equal(15, route.IntervalMinutes);
        Assert.False(route.Enabled);
    }

    [Fact]
    public async Task UpdateAsync_MissingRoute_ThrowsKeyNotFoundException()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var repository = new SyncRouteRepository(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateAsync(new SyncRoute()));
    }
}
