using System.Security.Cryptography;
using CostSharing.Core.Models;

namespace CostSharingApp.Services;

/// <summary>
/// Manages group invitations including creation, validation, and acceptance.
/// </summary>
public class InvitationService : IInvitationService
{
    private const int InvitationExpirationDays = 7;
    private const string BaseInvitationUrl = "costsharingapp://invite/";

    private readonly ICacheService cacheService;
    private readonly IGroupService groupService;
    private readonly IAuthService authService;
    private readonly INotificationService notificationService;
    private readonly ILoggingService loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationService"/> class.
    /// </summary>
    public InvitationService(
        ICacheService cacheService,
        IGroupService groupService,
        IAuthService authService,
        INotificationService notificationService,
        ILoggingService loggingService)
    {
        this.cacheService = cacheService;
        this.groupService = groupService;
        this.authService = authService;
        this.notificationService = notificationService;
        this.loggingService = loggingService;
    }

    /// <summary>
    /// Creates and sends an invitation via email.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="inviteeEmail">Invitee email.</param>
    /// <returns>Created invitation or null.</returns>
    public async Task<Invitation?> SendEmailInvitationAsync(Guid groupId, string inviteeEmail)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return null;
            }

            var group = await this.groupService.GetGroupAsync(groupId);
            if (group == null)
            {
                return null;
            }

            // Check if user is admin
            var members = await this.groupService.GetGroupMembersAsync(groupId);
            var isAdmin = members.Any(m => m.UserId == currentUser.Id && m.Role == GroupRole.Admin);
            if (!isAdmin)
            {
                this.loggingService.LogWarning($"User {currentUser.Id} not authorized to invite to group {groupId}");
                return null;
            }

            // Generate invitation
            var invitation = new Invitation
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                InvitedBy = currentUser.Id,
                InviteeContact = inviteeEmail,
                Status = InvitationStatus.Pending,
                SentAt = DateTime.UtcNow,
                Token = this.GenerateInvitationToken()
            };

            // Save invitation
            await this.cacheService.SaveAsync(invitation);

            // Send email
            var invitationLink = $"{BaseInvitationUrl}{invitation.Token}";
            var emailSent = await this.SendInvitationEmail(
                inviteeEmail,
                currentUser.Name,
                group.Name,
                invitationLink,
                DateTime.UtcNow.AddDays(InvitationExpirationDays));

            if (!emailSent)
            {
                this.loggingService.LogWarning($"Failed to send invitation email to {inviteeEmail}");
            }

            return invitation;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to send email invitation", ex);
            return null;
        }
    }

    /// <summary>
    /// Creates and sends an invitation via SMS.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="inviteePhone">Invitee phone (E.164 format).</param>
    /// <returns>Created invitation or null.</returns>
    public async Task<Invitation?> SendSmsInvitationAsync(Guid groupId, string inviteePhone)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return null;
            }

            var group = await this.groupService.GetGroupAsync(groupId);
            if (group == null)
            {
                return null;
            }

            // Check if user is admin
            var members = await this.groupService.GetGroupMembersAsync(groupId);
            var isAdmin = members.Any(m => m.UserId == currentUser.Id && m.Role == GroupRole.Admin);
            if (!isAdmin)
            {
                this.loggingService.LogWarning($"User {currentUser.Id} not authorized to invite to group {groupId}");
                return null;
            }

            // Generate invitation
            var invitation = new Invitation
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                InvitedBy = currentUser.Id,
                InviteeContact = inviteePhone,
                Status = InvitationStatus.Pending,
                SentAt = DateTime.UtcNow,
                Token = this.GenerateInvitationToken()
            };

            // Save invitation
            await this.cacheService.SaveAsync(invitation);

            // Send SMS
            var invitationLink = $"{BaseInvitationUrl}{invitation.Token}";
            var smsSent = await this.SendInvitationSms(
                inviteePhone,
                currentUser.Name,
                group.Name,
                invitationLink);

            if (!smsSent)
            {
                this.loggingService.LogWarning($"Failed to send invitation SMS to {inviteePhone}");
            }

            return invitation;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to send SMS invitation", ex);
            return null;
        }
    }

    /// <summary>
    /// Accepts an invitation and adds user to group.
    /// </summary>
    /// <param name="token">Invitation token.</param>
    /// <returns>Group ID if successful, null otherwise.</returns>
    public async Task<Guid?> AcceptInvitationAsync(string token)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                this.loggingService.LogWarning("Cannot accept invitation: User not authenticated");
                return null;
            }

            // Find invitation by token
            var allInvitations = await this.cacheService.GetAllAsync<Invitation>();
            var invitation = allInvitations.FirstOrDefault(i => i.Token == token);

            if (invitation == null)
            {
                this.loggingService.LogWarning($"Invitation not found: {token}");
                return null;
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                this.loggingService.LogWarning($"Invitation already {invitation.Status}: {token}");
                return null;
            }

            // Check expiration
            if (DateTime.UtcNow > invitation.SentAt.AddDays(InvitationExpirationDays))
            {
                invitation.Status = InvitationStatus.Expired;
                await this.cacheService.SaveAsync(invitation);
                this.loggingService.LogWarning($"Invitation expired: {token}");
                return null;
            }

            // Check if user already in group
            var members = await this.groupService.GetGroupMembersAsync(invitation.GroupId);
            if (members.Any(m => m.UserId == currentUser.Id))
            {
                this.loggingService.LogWarning($"User {currentUser.Id} already in group {invitation.GroupId}");
                return invitation.GroupId;
            }

            // Add user to group
            var newMember = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = invitation.GroupId,
                UserId = currentUser.Id,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow,
                AddedBy = invitation.InvitedBy
            };

            await this.cacheService.SaveAsync(newMember);

            // Update invitation status
            invitation.Status = InvitationStatus.Accepted;
            invitation.RespondedAt = DateTime.UtcNow;
            await this.cacheService.SaveAsync(invitation);

            this.loggingService.LogInfo($"User {currentUser.Id} accepted invitation to group {invitation.GroupId}");
            return invitation.GroupId;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to accept invitation", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets all pending invitations for a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>List of pending invitations.</returns>
    public async Task<List<Invitation>> GetPendingInvitationsAsync(Guid groupId)
    {
        try
        {
            var allInvitations = await this.cacheService.GetAllAsync<Invitation>();
            return allInvitations
                .Where(i => i.GroupId == groupId && i.Status == InvitationStatus.Pending)
                .ToList();
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to get pending invitations for group {groupId}", ex);
            return new List<Invitation>();
        }
    }

    /// <summary>
    /// Cancels a pending invitation.
    /// </summary>
    /// <param name="invitationId">Invitation ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> CancelInvitationAsync(Guid invitationId)
    {
        try
        {
            var invitation = await this.cacheService.GetAsync<Invitation>(invitationId);
            if (invitation == null || invitation.Status != InvitationStatus.Pending)
            {
                return false;
            }

            invitation.Status = InvitationStatus.Declined;
            invitation.RespondedAt = DateTime.UtcNow;
            await this.cacheService.SaveAsync(invitation);

            this.loggingService.LogInfo($"Invitation {invitationId} cancelled");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to cancel invitation", ex);
            return false;
        }
    }

    /// <summary>
    /// Resends an invitation.
    /// </summary>
    /// <param name="invitationId">Invitation ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> ResendInvitationAsync(Guid invitationId)
    {
        try
        {
            var invitation = await this.cacheService.GetAsync<Invitation>(invitationId);
            if (invitation == null)
            {
                return false;
            }

            var group = await this.groupService.GetGroupAsync(invitation.GroupId);
            if (group == null)
            {
                return false;
            }

            var inviter = await this.cacheService.GetAsync<User>(invitation.InvitedBy);
            if (inviter == null)
            {
                return false;
            }

            // Resend based on contact type
            var invitationLink = $"{BaseInvitationUrl}{invitation.Token}";
            bool sent;

            if (invitation.InviteeContact.Contains("@"))
            {
                // Email
                sent = await this.SendInvitationEmail(
                    invitation.InviteeContact,
                    inviter.Name,
                    group.Name,
                    invitationLink,
                    DateTime.UtcNow.AddDays(InvitationExpirationDays));
            }
            else
            {
                // SMS
                sent = await this.SendInvitationSms(
                    invitation.InviteeContact,
                    inviter.Name,
                    group.Name,
                    invitationLink);
            }

            if (sent)
            {
                invitation.SentAt = DateTime.UtcNow;
                invitation.Status = InvitationStatus.Pending;
                await this.cacheService.SaveAsync(invitation);
            }

            return sent;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to resend invitation", ex);
            return false;
        }
    }

    /// <summary>
    /// Generates a cryptographically secure invitation token.
    /// </summary>
    /// <returns>Base64-encoded token.</returns>
    private string GenerateInvitationToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // 256 bits
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    /// <summary>
    /// Sends invitation email.
    /// </summary>
    private async Task<bool> SendInvitationEmail(
        string toEmail,
        string inviterName,
        string groupName,
        string invitationLink,
        DateTime expirationDate)
    {
        var subject = $"{inviterName} invited you to join '{groupName}' on Cost Sharing App";

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{ background-color: #512BD4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block; margin: 20px 0; }}
        .footer {{ font-size: 12px; color: #666; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>You're Invited!</h2>
        <p>Hi there,</p>
        <p><strong>{inviterName}</strong> has invited you to join the group <strong>'{groupName}'</strong> on Cost Sharing App.</p>
        <p>Click the button below to accept the invitation:</p>
        <a href=""{invitationLink}"" class=""button"">Accept Invitation</a>
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all;"">{invitationLink}</p>
        <p>This invitation expires on <strong>{expirationDate:MMMM dd, yyyy}</strong>.</p>
        <div class=""footer"">
            <p>If you didn't expect this invitation, you can safely ignore this email.</p>
        </div>
    </div>
</body>
</html>";

        var plainTextContent = $@"
You're Invited!

{inviterName} has invited you to join the group '{groupName}' on Cost Sharing App.

Click this link to accept the invitation:
{invitationLink}

This invitation expires on {expirationDate:MMMM dd, yyyy}.

If you didn't expect this invitation, you can safely ignore this email.
";

        return await this.notificationService.SendEmailAsync(toEmail, subject, htmlContent, plainTextContent);
    }

    /// <summary>
    /// Sends invitation SMS.
    /// </summary>
    private async Task<bool> SendInvitationSms(
        string toPhone,
        string inviterName,
        string groupName,
        string invitationLink)
    {
        // SMS must be under 160 chars for single message
        var message = $"{inviterName} invited you to '{groupName}' on Cost Sharing App. Join: {invitationLink}";

        if (message.Length > 160)
        {
            // Truncate group name if necessary
            var maxGroupNameLength = 160 - ($"{inviterName} invited you to '' on Cost Sharing App. Join: {invitationLink}".Length);
            var truncatedGroupName = groupName.Length > maxGroupNameLength
                ? groupName.Substring(0, maxGroupNameLength - 3) + "..."
                : groupName;

            message = $"{inviterName} invited you to '{truncatedGroupName}' on Cost Sharing App. Join: {invitationLink}";
        }

        return await this.notificationService.SendSmsAsync(toPhone, message);
    }
}

/// <summary>
/// Interface for invitation service.
/// </summary>
public interface IInvitationService
{
    /// <summary>
    /// Sends email invitation.
    /// </summary>
    Task<Invitation?> SendEmailInvitationAsync(Guid groupId, string inviteeEmail);

    /// <summary>
    /// Sends SMS invitation.
    /// </summary>
    Task<Invitation?> SendSmsInvitationAsync(Guid groupId, string inviteePhone);

    /// <summary>
    /// Accepts invitation.
    /// </summary>
    Task<Guid?> AcceptInvitationAsync(string token);

    /// <summary>
    /// Gets pending invitations for group.
    /// </summary>
    Task<List<Invitation>> GetPendingInvitationsAsync(Guid groupId);

    /// <summary>
    /// Cancels invitation.
    /// </summary>
    Task<bool> CancelInvitationAsync(Guid invitationId);

    /// <summary>
    /// Resends invitation.
    /// </summary>
    Task<bool> ResendInvitationAsync(Guid invitationId);
}
