namespace LaundryMS.Web.Models.Entities;

/// <summary>
/// Local application user for password-based sign-in (e.g. role path &quot;users&quot;).
/// Password is stored as a BCrypt hash in <see cref="PasswordHash"/>.
/// </summary>
public class AppUser
{
    public ulong Id { get; set; }

    /// <summary>Display name for UI and JWT name claims.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique sign-in address (case-insensitive match should be done in application code).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hash of the password (never store plain text).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Authorization role for JWT: typically MANAGER or ADMIN.</summary>
    public string Role { get; set; } = "MANAGER";

    /// <summary>External tenant id (same as JWT <c>CustomerId</c> / PulsePoint company scope). Not a FK to <see cref="Customer.Id"/>.</summary>
    public ulong CustomerId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
