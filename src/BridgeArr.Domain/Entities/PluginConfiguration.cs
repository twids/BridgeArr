namespace BridgeArr.Domain.Entities;

/// <summary>
/// Represents plugin-specific configuration stored as JSON.
/// </summary>
public class PluginConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IntegrationId { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Integration? Integration { get; set; }
}
