using SQLite;

namespace CostSharing.Core.Models;

/// <summary>
/// Represents the type of user account.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Device-based account with auto-generated email (@device.local).
    /// </summary>
    Device = 0,

    /// <summary>
    /// Email-based account with user-provided email and password.
    /// </summary>
    Email = 1,
}

/// <summary>
/// Represents an application user with authentication credentials and profile information.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [PrimaryKey]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the primary contact and login email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional phone number in E.164 format.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets the hashed password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name (1-100 chars).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last successful login timestamp.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether email is verified.
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// Gets or sets the account type (Device or Email).
    /// </summary>
    public AccountType AccountType { get; set; } = AccountType.Device;

    /// <summary>
    /// Gets a value indicating whether this is a device-based account.
    /// </summary>
    [Ignore]
    public bool IsDeviceAccount => this.Email.EndsWith("@device.local", StringComparison.OrdinalIgnoreCase);
}
