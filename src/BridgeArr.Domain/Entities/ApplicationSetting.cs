namespace BridgeArr.Domain.Entities;

/// <summary>
/// Represents an application-wide setting.
/// </summary>
public class ApplicationSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
