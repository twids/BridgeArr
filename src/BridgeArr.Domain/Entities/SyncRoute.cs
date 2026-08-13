namespace BridgeArr.Domain.Entities;

/// <summary>Defines a user-configured synchronization path and schedule.</summary>
public class SyncRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid SourceIntegrationId { get; set; }
    public Guid TargetIntegrationId { get; set; }
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
    public DateTimeOffset? LastQueuedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Integration? SourceIntegration { get; set; }
    public Integration? TargetIntegration { get; set; }
}
