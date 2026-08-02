using BridgeArr.Application.Interfaces;
using BridgeArr.Application.Services;
using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BridgeArr.UnitTests.Services;

public class SyncServiceTests
{
    [Fact]
    public async Task RequestSyncAsync_Persists_And_Enqueues_Job()
    {
        var integrations = new Dictionary<Guid, Integration>();
        var syncJobRepository = new InMemorySyncJobRepository();
        var queue = new RecordingSyncQueue();
        var service = new SyncService(
            new InMemoryIntegrationRepository(integrations),
            syncJobRepository,
            queue,
            [],
            [],
            NullLogger<SyncService>.Instance);

        var job = await service.RequestSyncAsync(Guid.NewGuid(), Guid.NewGuid(), "payload");

        Assert.Equal(SyncJobStatus.Queued, job.Status);
        Assert.Single(syncJobRepository.Jobs);
        Assert.Single(queue.EnqueuedJobs);
    }

    [Fact]
    public async Task ExecuteSyncAsync_Updates_Target_Labels_For_Matching_Items()
    {
        var sourceIntegration = new Integration { Id = Guid.NewGuid(), Name = "Radarr", PluginType = "radarr", Enabled = true };
        var targetIntegration = new Integration { Id = Guid.NewGuid(), Name = "Plex", PluginType = "plex", Enabled = true };
        var integrations = new Dictionary<Guid, Integration>
        {
            [sourceIntegration.Id] = sourceIntegration,
            [targetIntegration.Id] = targetIntegration
        };

        var queue = new RecordingSyncQueue();
        var syncJobRepository = new InMemorySyncJobRepository();
        var target = new FakeMediaTarget();
        var service = new SyncService(
            new InMemoryIntegrationRepository(integrations),
            syncJobRepository,
            queue,
            [new FakeMediaSource()],
            [target],
            NullLogger<SyncService>.Instance);

        var job = await syncJobRepository.AddAsync(new SyncJob
        {
            SourceIntegrationId = sourceIntegration.Id,
            TargetIntegrationId = targetIntegration.Id,
            Status = SyncJobStatus.Queued
        });

        await service.ExecuteSyncAsync(job);

        Assert.Equal(SyncJobStatus.Completed, job.Status);
        Assert.Single(target.SetLabelsCalls);
        Assert.Equal(["hd", "favorite"], target.SetLabelsCalls[0].Labels);
    }

    private sealed class InMemoryIntegrationRepository : IIntegrationRepository
    {
        private readonly Dictionary<Guid, Integration> _integrations;

        public InMemoryIntegrationRepository(Dictionary<Guid, Integration> integrations)
        {
            _integrations = integrations;
        }

        public Task<Integration> AddAsync(Integration integration, CancellationToken cancellationToken = default)
        {
            _integrations[integration.Id] = integration;
            return Task.FromResult(integration);
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _integrations.Remove(id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Integration>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Integration>>(_integrations.Values.ToList());

        public Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_integrations.TryGetValue(id, out var integration) ? integration : null);

        public Task<IReadOnlyList<Integration>> GetEnabledAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Integration>>(_integrations.Values.Where(x => x.Enabled).ToList());

        public Task<Integration> UpdateAsync(Integration integration, CancellationToken cancellationToken = default)
        {
            _integrations[integration.Id] = integration;
            return Task.FromResult(integration);
        }
    }

    private sealed class InMemorySyncJobRepository : ISyncJobRepository
    {
        public List<SyncJob> Jobs { get; } = new();

        public Task<SyncJob> AddAsync(SyncJob job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return Task.FromResult(job);
        }

        public Task<SyncJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Jobs.FirstOrDefault(j => j.Id == id));

        public Task<IReadOnlyList<SyncJob>> GetPendingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SyncJob>>(Jobs.Where(j => j.Status == SyncJobStatus.Queued).ToList());

        public Task<IReadOnlyList<SyncJob>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SyncJob>>(Jobs.Take(count).ToList());

        public Task<SyncJob> UpdateAsync(SyncJob job, CancellationToken cancellationToken = default)
            => Task.FromResult(job);
    }

    private sealed class RecordingSyncQueue : ISyncQueue
    {
        public List<SyncJob> EnqueuedJobs { get; } = new();

        public ValueTask<SyncJob?> DequeueAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<SyncJob?>(EnqueuedJobs.FirstOrDefault());

        public ValueTask EnqueueAsync(SyncJob job, CancellationToken cancellationToken = default)
        {
            EnqueuedJobs.Add(job);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeMediaSource : IMediaSource
    {
        public string PluginType => "radarr";
        public string DisplayName => "Radarr";
        public string Version => "1.0.0";
        public PluginCapabilities Capabilities => PluginCapabilities.MediaSource;

        public Task<IReadOnlyList<MediaItem>> GetAllMediaAsync(string configurationJson, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MediaItem>>([
                new MediaItem
                {
                    Title = "Inception",
                    Year = 2010,
                    Type = MediaType.Movie,
                    Tags = [new Tag { Name = "hd" }, new Tag { Name = "favorite" }]
                }
            ]);

        public Task<MediaItem?> GetMediaByIdAsync(string sourceId, string configurationJson, CancellationToken cancellationToken = default)
            => Task.FromResult<MediaItem?>(null);

        public Task<IReadOnlyList<Tag>> GetTagsAsync(string configurationJson, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Tag>>([]);

        public Task<bool> TestConnectionAsync(string configurationJson, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class FakeMediaTarget : IMediaTarget
    {
        public string PluginType => "plex";
        public string DisplayName => "Plex";
        public string Version => "1.0.0";
        public PluginCapabilities Capabilities => PluginCapabilities.MediaTarget;
        public List<(string Id, IReadOnlyList<string> Labels)> SetLabelsCalls { get; } = new();

        public Task<bool> AddToCollectionAsync(string targetItemId, string collectionName, string configurationJson, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<string?> FindMediaAsync(MediaItem mediaItem, string configurationJson, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("plex-1");

        public Task<IReadOnlyList<string>> GetLabelsAsync(string targetItemId, string configurationJson, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<bool> SetLabelsAsync(string targetItemId, IReadOnlyList<string> labels, string configurationJson, CancellationToken cancellationToken = default)
        {
            SetLabelsCalls.Add((targetItemId, labels));
            return Task.FromResult(true);
        }

        public Task<bool> TestConnectionAsync(string configurationJson, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
