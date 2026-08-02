namespace BridgeArr.Domain.Entities;

/// <summary>
/// Represents a configured integration with an external system.
/// </summary>
public class Integration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string PluginType { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
