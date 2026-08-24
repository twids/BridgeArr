using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.Data;
using BridgeArr.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BridgeArr.IntegrationTests.Repositories;

public class IntegrationRepositoryTests
{
    [Fact]
    public async Task UpdateAsync_TrackedIntegrationAndDetachedChanges_UpdatesExistingIntegration()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var integration = new Integration { Name = "Radarr", PluginType = "radarr", Enabled = true, ConfigurationJson = "{\"url\":\"old\"}" };
        db.Integrations.Add(integration);
        await db.SaveChangesAsync();
        var repository = new IntegrationRepository(db);
        await repository.GetAllAsync();
        var changed = new Integration { Id = integration.Id, Name = "Movies", PluginType = "radarr", Enabled = false, ConfigurationJson = "{\"url\":\"new\"}", UpdatedAt = DateTimeOffset.UtcNow };

        var updated = await repository.UpdateAsync(changed);

        Assert.Same(integration, updated);
        Assert.Equal("Movies", integration.Name);
        Assert.False(integration.Enabled);
        Assert.Equal("{\"url\":\"new\"}", integration.ConfigurationJson);
    }

    [Fact]
    public async Task UpdateAsync_MissingIntegration_ThrowsKeyNotFoundException()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var repository = new IntegrationRepository(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateAsync(new Integration()));
    }
}
