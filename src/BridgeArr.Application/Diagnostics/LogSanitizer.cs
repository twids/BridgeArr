namespace BridgeArr.Application.Diagnostics;

/// <summary>
/// Sanitizes untrusted values before emitting them to logs.
/// </summary>
public static class LogSanitizer
{
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Trim();
    }
}
