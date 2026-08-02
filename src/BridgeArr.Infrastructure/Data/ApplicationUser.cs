using Microsoft.AspNetCore.Identity;

namespace BridgeArr.Infrastructure.Data;

/// <summary>
/// BridgeArr application user extending ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Gets or sets whether the user must change their password on next login.</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Gets or sets the user's display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets when the user was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
