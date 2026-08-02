using BridgeArr.Domain.Entities;

namespace BridgeArr.Plugins.Abstractions;

/// <summary>
/// Defines the contract for a media source plugin (e.g., Radarr, Sonarr).
/// </summary>
public interface IMediaSource : IPlugin
{
    /// <summary>
    /// Tests the connection to the source system.
    /// </summary>
    Task<bool> TestConnectionAsync(string configurationJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all media items from the source.
    /// </summary>
    Task<IReadOnlyList<MediaItem>> GetAllMediaAsync(string configurationJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single media item by its source ID.
    /// </summary>
    Task<MediaItem?> GetMediaByIdAsync(string sourceId, string configurationJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tags defined in the source system.
    /// </summary>
    Task<IReadOnlyList<Tag>> GetTagsAsync(string configurationJson, CancellationToken cancellationToken = default);
}
