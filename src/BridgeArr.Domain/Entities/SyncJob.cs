using BridgeArr.Domain.Enums;

namespace BridgeArr.Domain.Entities;

/// <summary>
/// Represents a synchronization job.
/// </summary>
public class SyncJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceIntegrationId { get; set; }
    public Guid TargetIntegrationId { get; set; }
    public SyncJobStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Payload { get; set; }
    public Integration? SourceIntegration { get; set; }
    public Integration? TargetIntegration { get; set; }
}
