using SQLite;

namespace CostSharing.Core.Models;

/// <summary>
/// Represents a group invitation sent to a user via email or SMS.
/// </summary>
public class Invitation
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [PrimaryKey]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the group reference.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the inviter's user ID.
    /// </summary>
    public Guid InvitedBy { get; set; }

    /// <summary>
    /// Gets or sets the invitee's email or phone.
    /// </summary>
    public string InviteeContact { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invitation status.
    /// </summary>
    public InvitationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when invitation was sent.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Gets or sets when invitation was accepted/declined.
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Gets or sets unique token for invitation link.
    /// </summary>
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Defines invitation states.
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    /// Invitation sent, awaiting response.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Invitation accepted, user joined group.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Invitation declined by invitee.
    /// </summary>
    Declined = 2,

    /// <summary>
    /// Invitation expired (not responded within timeframe).
    /// </summary>
    Expired = 3
}
