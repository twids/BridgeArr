using System.Threading.Channels;
using BridgeArr.Application.Interfaces;
using BridgeArr.Domain.Entities;

namespace BridgeArr.Infrastructure.Queue;

/// <summary>
/// In-memory sync queue using System.Threading.Channels.
/// For production use, consider replacing with a persistent queue.
/// </summary>
public class InMemorySyncQueue : ISyncQueue
{
    private readonly Channel<SyncJob> _channel = Channel.CreateUnbounded<SyncJob>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public async ValueTask EnqueueAsync(SyncJob job, CancellationToken cancellationToken = default)
        => await _channel.Writer.WriteAsync(job, cancellationToken);

    public async ValueTask<SyncJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
