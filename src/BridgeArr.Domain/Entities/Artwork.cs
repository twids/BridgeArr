using BridgeArr.Domain.Enums;

namespace BridgeArr.Domain.Entities;

/// <summary>
/// Represents artwork associated with a media item.
/// </summary>
public class Artwork
{
    /// <summary>Gets or sets the artwork type (Poster, Banner, Fanart, etc.).</summary>
    public ArtworkType Type { get; set; }

    /// <summary>Gets or sets the URL of the artwork.</summary>
    public string Url { get; set; } = string.Empty;
}
