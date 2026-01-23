// <copyright file="IGmailInvitationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharing.Core.Interfaces;

/// <summary>
/// Service interface for sending group invitations via Gmail API.
/// </summary>
public interface IGmailInvitationService
{
    /// <summary>
    /// Checks if the user has authorized Gmail access.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if authorized, false otherwise.</returns>
    Task<bool> IsGmailAuthorizedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a group invitation email via Gmail.
    /// </summary>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="recipientName">The recipient's name.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="inviterName">The name of the person sending the invitation.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing success status, optional error message, and optional message ID.</returns>
    Task<(bool Success, string? ErrorMessage, string? MessageId)> SendInvitationAsync(
        string recipientEmail,
        string recipientName,
        string groupName,
        string inviterName,
        Guid userId,
        CancellationToken cancellationToken = default);
}
