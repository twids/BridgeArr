using BridgeArr.Application.Interfaces;
using BridgeArr.Application.Services;
using BridgeArr.Domain.Entities;
using BridgeArr.Infrastructure.BackgroundServices;
using BridgeArr.Infrastructure.Data;
using BridgeArr.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BridgeArr.IntegrationTests.BackgroundServices;

public class ScheduledSyncWorkerBehaviorTests
{
    [Fact]
    public async Task QueueDueRoutesAsync_DueRoute_QueuesOnlyOneActiveJob()
    {
        var services = new ServiceCollection();
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName, databaseRoot));
        services.AddLogging();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<ISyncJobRepository, SyncJobRepository>();
        services.AddScoped<ISyncRouteRepository, SyncRouteRepository>();
        services.AddScoped<SyncService>();
        services.AddScoped<SyncRouteService>();
        var queue = new RecordingSyncQueue();
        services.AddSingleton<ISyncQueue>(queue);
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var source = new Integration { Name = "Sonarr", PluginType = "sonarr", Enabled = true };
            var target = new Integration { Name = "Plex", PluginType = "plex", Enabled = true };
            db.Integrations.AddRange(source, target);
            await db.SaveChangesAsync();
            db.SyncRoutes.Add(new SyncRoute
            {
                Name = "TV tags",
                SourceIntegrationId = source.Id,
                TargetIntegrationId = target.Id,
                Enabled = true,
                IntervalMinutes = 60
            });
            await db.SaveChangesAsync();
        }

        await using (var checkScope = provider.CreateAsyncScope())
            Assert.Single(await checkScope.ServiceProvider.GetRequiredService<ISyncRouteRepository>().GetEnabledAsync());

        var worker = new ScheduledSyncWorker(provider, NullLogger<ScheduledSyncWorker>.Instance);
        await worker.QueueDueRoutesAsync();
        await worker.QueueDueRoutesAsync();

        await using var verificationScope = provider.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await dbContext.SyncJobs.ToListAsync());
        Assert.Single(queue.Jobs);
        Assert.NotNull((await dbContext.SyncRoutes.SingleAsync()).LastQueuedAt);
    }

    private sealed class RecordingSyncQueue : ISyncQueue
    {
        public List<SyncJob> Jobs { get; } = [];
        public ValueTask EnqueueAsync(SyncJob job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return ValueTask.CompletedTask;
        }
        public ValueTask<SyncJob?> DequeueAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<SyncJob?>(null);
    }
}

