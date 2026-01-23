// <copyright file="IInvitationLinkingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CostSharing.Core.Models;

namespace CostSharing.Core.Interfaces;

/// <summary>
/// Service for managing group invitations and linking memberships.
/// </summary>
public interface IInvitationLinkingService
{
    /// <summary>
    /// Invites a user to a group by email address.
    /// Creates a GroupMember if user exists, or PendingInvitation if not.
    /// </summary>
    /// <param name="groupId">Target group.</param>
    /// <param name="invitedEmail">Email to invite.</param>
    /// <param name="inviterUserId">User sending the invitation.</param>
    /// <param name="sendEmail">Whether to send Gmail notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with success status, type (member/pending), and message.</returns>
    Task<InvitationResult> InviteToGroupAsync(
        Guid groupId,
        string invitedEmail,
        Guid inviterUserId,
        bool sendEmail = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links all pending invitations for an email to a user account.
    /// Called after registration or login.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of groups linked.</returns>
    Task<int> LinkPendingInvitationsAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending invitations for a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending invitations.</returns>
    Task<List<PendingInvitation>> GetPendingInvitationsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending invitation.
    /// </summary>
    /// <param name="invitationId">Invitation ID.</param>
    /// <param name="cancelledByUserId">User cancelling (must be admin).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if cancelled.</returns>
    Task<bool> CancelInvitationAsync(
        Guid invitationId,
        Guid cancelledByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already a member or has a pending invitation.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="email">Email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if already member or pending.</returns>
    Task<bool> IsAlreadyMemberOrPendingAsync(
        Guid groupId,
        string email,
        CancellationToken cancellationToken = default);
}
