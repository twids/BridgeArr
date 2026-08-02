using BridgeArr.Domain.Entities;

namespace BridgeArr.Plugins.Abstractions;

/// <summary>
/// Defines the contract for a media target plugin (e.g., Plex, Jellyfin).
/// </summary>
public interface IMediaTarget : IPlugin
{
    /// <summary>
    /// Tests the connection to the target system.
    /// </summary>
    Task<bool> TestConnectionAsync(string configurationJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a media item in the target system using external IDs.
    /// </summary>
    Task<string?> FindMediaAsync(MediaItem mediaItem, string configurationJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current labels for a media item in the target.
    /// </summary>
    Task<IReadOnlyList<string>> GetLabelsAsync(string targetItemId, string configurationJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all labels on a media item in the target system.
    /// </summary>
    Task<bool> SetLabelsAsync(string targetItemId, IReadOnlyList<string> labels, string configurationJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a collection to a media item in the target system.
    /// </summary>
    Task<bool> AddToCollectionAsync(string targetItemId, string collectionName, string configurationJson, CancellationToken cancellationToken = default);
}
