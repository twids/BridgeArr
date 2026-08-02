namespace BridgeArr.Application.Events;

/// <summary>Event raised when a media item is imported.</summary>
public record MediaImportedEvent(Guid MediaItemId, string SourcePlugin, DateTimeOffset OccurredAt);

/// <summary>Event raised when a media item is updated.</summary>
public record MediaUpdatedEvent(Guid MediaItemId, string SourcePlugin, DateTimeOffset OccurredAt);

/// <summary>Event raised when tags change on a media item.</summary>
public record TagsChangedEvent(Guid MediaItemId, IReadOnlyList<string> Tags, DateTimeOffset OccurredAt);

/// <summary>Event raised when a sync is requested.</summary>
public record SyncRequestedEvent(Guid SyncJobId, Guid SourceIntegrationId, Guid TargetIntegrationId, DateTimeOffset OccurredAt);

/// <summary>Event raised when a sync completes successfully.</summary>
public record SyncCompletedEvent(Guid SyncJobId, DateTimeOffset OccurredAt);

/// <summary>Event raised when a sync fails.</summary>
public record SyncFailedEvent(Guid SyncJobId, string ErrorMessage, DateTimeOffset OccurredAt);
