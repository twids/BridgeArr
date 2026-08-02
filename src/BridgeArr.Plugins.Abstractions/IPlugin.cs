namespace BridgeArr.Plugins.Abstractions;

/// <summary>
/// Defines the base contract for all BridgeArr plugins.
/// </summary>
public interface IPlugin
{
    /// <summary>Gets the unique plugin type identifier.</summary>
    string PluginType { get; }

    /// <summary>Gets the display name of the plugin.</summary>
    string DisplayName { get; }

    /// <summary>Gets the plugin version.</summary>
    string Version { get; }

    /// <summary>Gets the capabilities this plugin supports.</summary>
    PluginCapabilities Capabilities { get; }
}

/// <summary>
/// Describes the capabilities of a plugin.
/// </summary>
[Flags]
public enum PluginCapabilities
{
    None = 0,
    MediaSource = 1 << 0,
    MediaTarget = 1 << 1,
    WebhookHandler = 1 << 2
}
