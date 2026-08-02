namespace BridgeArr.Domain.Enums;

/// <summary>
/// Represents the execution state of a synchronization job.
/// </summary>
public enum SyncJobStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
