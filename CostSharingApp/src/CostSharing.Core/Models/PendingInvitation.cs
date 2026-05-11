// <copyright file="PendingInvitation.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SQLite;

namespace CostSharing.Core.Models;

/// <summary>
/// Tracks invitations sent to email addresses that do not yet have user accounts.
/// </summary>
public class PendingInvitation
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [PrimaryKey]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the group the user is invited to.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the normalized (lowercase) email address of the invitee.
    /// </summary>
    public string InvitedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID of who sent the invitation.
    /// </summary>
    public Guid InvitedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the invitation was created.
    /// </summary>
    public DateTime InvitedAt { get; set; }

    /// <summary>
    /// Gets or sets the current status of the invitation.
    /// </summary>
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// <summary>
    /// Gets or sets when the invitation was accepted (user signed up).
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID after account creation.
    /// </summary>
    public Guid? LinkedUserId { get; set; }
}
