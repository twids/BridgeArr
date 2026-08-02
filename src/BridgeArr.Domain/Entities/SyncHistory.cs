namespace BridgeArr.Domain.Entities;

/// <summary>
/// Represents a historical record of a sync job execution.
/// </summary>
public class SyncHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SyncJobId { get; set; }
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public SyncJob? SyncJob { get; set; }
}
