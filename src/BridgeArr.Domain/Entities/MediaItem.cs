using BridgeArr.Domain.Enums;

namespace BridgeArr.Domain.Entities;

/// <summary>
/// Represents a media item (movie, series, etc.) in the BridgeArr system.
/// </summary>
public class MediaItem
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the release year.</summary>
    public int? Year { get; set; }

    /// <summary>Gets or sets the media type (Movie, Series, etc.).</summary>
    public MediaType Type { get; set; }

    /// <summary>Gets or sets external IDs from various providers.</summary>
    public List<ExternalId> ExternalIds { get; set; } = new();

    /// <summary>Gets or sets tags associated with this item.</summary>
    public List<Tag> Tags { get; set; } = new();

    /// <summary>Gets or sets the genres.</summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>Gets or sets the collections this item belongs to.</summary>
    public List<string> Collections { get; set; } = new();

    /// <summary>Gets or sets labels applied to this item.</summary>
    public List<string> Labels { get; set; } = new();

    /// <summary>Gets or sets artwork URLs.</summary>
    public List<Artwork> Artwork { get; set; } = new();

    /// <summary>Gets or sets when the item was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the item was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
