namespace BridgeArr.Domain.Entities;

/// <summary>
/// Represents an external identifier from a specific provider.
/// </summary>
public class ExternalId
{
    /// <summary>Gets or sets the provider name (e.g., "tmdb", "imdb", "tvdb").</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier value.</summary>
    public string Value { get; set; } = string.Empty;
}
