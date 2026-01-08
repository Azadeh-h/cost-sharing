namespace CostSharingApp.Services;

/// <summary>
/// Interface for Google-based invitation service.
/// </summary>
public interface IGoogleInvitationService
{
    /// <summary>
    /// Sends an invitation email to join a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="inviteeEmail">Email address of person to invite.</param>
    /// <param name="driveFileId">Google Drive file ID for the group data.</param>
    /// <returns>True if invitation sent successfully.</returns>
    Task<bool> SendInvitationAsync(Guid groupId, string inviteeEmail, string driveFileId);

    /// <summary>
    /// Handles incoming deep link for group invitation.
    /// </summary>
    /// <param name="deepLink">Deep link URL (e.g., costsharingapp://join?groupId=xxx).</param>
    /// <returns>Group ID if valid, null otherwise.</returns>
    Task<Guid?> HandleInvitationDeepLinkAsync(string deepLink);

    /// <summary>
    /// Accepts a group invitation by downloading group data from Drive.
    /// </summary>
    /// <param name="groupId">Group ID to join.</param>
    /// <returns>True if successfully joined.</returns>
    Task<bool> AcceptInvitationAsync(Guid groupId);
}
