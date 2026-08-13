using BridgeArr.Application.Interfaces;
using BridgeArr.Application.Services;
using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.BackgroundServices;
using BridgeArr.Infrastructure.Data;
using BridgeArr.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BridgeArr.IntegrationTests.BackgroundServices;

public class ScheduledSyncWorkerBehaviorTests
{
    [Fact]
    public async Task QueueSyncIfNeededAsync_EnabledIntegrations_QueuesOnlyOneActiveJob()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddLogging();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<ISyncJobRepository, SyncJobRepository>();
        services.AddScoped<SyncService>();
        var queue = new RecordingSyncQueue();
        services.AddSingleton<ISyncQueue>(queue);
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Integrations.AddRange(
                new Integration { Name = "Radarr", PluginType = "radarr", Enabled = true },
                new Integration { Name = "Plex", PluginType = "plex", Enabled = true });
            await db.SaveChangesAsync();
        }

        var worker = new ScheduledSyncWorker(
            provider,
            new ConfigurationBuilder().Build(),
            NullLogger<ScheduledSyncWorker>.Instance);
        await worker.QueueSyncIfNeededAsync();
        await worker.QueueSyncIfNeededAsync();

        await using var verificationScope = provider.CreateAsyncScope();
        var jobs = await verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SyncJobs.ToListAsync();
        Assert.Single(jobs);
        Assert.Single(queue.Jobs);
    }

    private sealed class RecordingSyncQueue : ISyncQueue
    {
        public List<SyncJob> Jobs { get; } = [];
        public ValueTask EnqueueAsync(SyncJob job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return ValueTask.CompletedTask;
        }

        public ValueTask<SyncJob?> DequeueAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<SyncJob?>(null);
    }
}
