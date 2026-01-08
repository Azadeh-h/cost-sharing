
using System.Text;
using CostSharing.Core.Models;
using Google.Apis.Gmail.v1.Data;

namespace CostSharingApp.Services;
/// <summary>
/// Service for sending group invitations via Gmail API.
/// </summary>
public class GoogleInvitationService : IGoogleInvitationService
{
    private readonly IGoogleAuthService googleAuthService;
    private readonly IGoogleDriveService googleDriveService;
    private readonly IGroupService groupService;
    private readonly IAuthService authService;
    private readonly ILoggingService loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleInvitationService"/> class.
    /// </summary>
    public GoogleInvitationService(
        IGoogleAuthService googleAuthService,
        IGoogleDriveService googleDriveService,
        IGroupService groupService,
        IAuthService authService,
        ILoggingService loggingService)
    {
        this.googleAuthService = googleAuthService;
        this.googleDriveService = googleDriveService;
        this.groupService = groupService;
        this.authService = authService;
        this.loggingService = loggingService;
    }

    /// <summary>
    /// Sends an invitation email to join a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="inviteeEmail">Email address of person to invite.</param>
    /// <param name="driveFileId">Google Drive file ID for the group data.</param>
    /// <returns>True if invitation sent successfully.</returns>
    public async Task<bool> SendInvitationAsync(Guid groupId, string inviteeEmail, string driveFileId)
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            throw new InvalidOperationException("User not authenticated");
        }

        try
        {
            var group = await this.groupService.GetGroupAsync(groupId);
            if (group == null)
            {
                this.loggingService.LogError($"Group {groupId} not found");
                return false;
            }

            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                this.loggingService.LogError("Current user not found");
                return false;
            }

            // Share Drive file with invitee
            await this.googleDriveService.ShareGroupFileAsync(driveFileId, inviteeEmail);
            this.loggingService.LogInfo($"Shared Drive file {driveFileId} with {inviteeEmail}");

            // Create and send invitation email
            var emailBody = this.CreateInvitationEmailBody(group.Name, currentUser.Email ?? "a friend", groupId);
            var emailSubject = $"You're invited to join \"{group.Name}\" on Cost Sharing App";

            var success = await this.SendEmailAsync(inviteeEmail, emailSubject, emailBody);
            
            if (success)
            {
                this.loggingService.LogInfo($"Invitation sent to {inviteeEmail} for group {groupId}");
            }
            else
            {
                this.loggingService.LogError($"Failed to send invitation to {inviteeEmail}");
            }

            return success;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Error sending invitation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Handles incoming deep link for group invitation.
    /// </summary>
    /// <param name="deepLink">Deep link URL (e.g., costsharingapp://join?groupId=xxx).</param>
    /// <returns>Group ID if valid, null otherwise.</returns>
    public async Task<Guid?> HandleInvitationDeepLinkAsync(string deepLink)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deepLink))
            {
                return null;
            }

            // Parse deep link: costsharingapp://join?groupId=xxx
            var uri = new Uri(deepLink);
            if (uri.Scheme != "costsharingapp" || uri.Host != "join")
            {
                this.loggingService.LogWarning($"Invalid deep link format: {deepLink}");
                return null;
            }

            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var groupIdStr = query["groupId"];
            
            if (string.IsNullOrWhiteSpace(groupIdStr) || !Guid.TryParse(groupIdStr, out var groupId))
            {
                this.loggingService.LogWarning($"Invalid groupId in deep link: {groupIdStr}");
                return null;
            }

            this.loggingService.LogInfo($"Valid invitation deep link for group {groupId}");
            return groupId;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Error parsing deep link: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Accepts a group invitation by downloading group data from Drive.
    /// </summary>
    /// <param name="groupId">Group ID to join.</param>
    /// <returns>True if successfully joined.</returns>
    public async Task<bool> AcceptInvitationAsync(Guid groupId)
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            throw new InvalidOperationException("User not authenticated");
        }

        try
        {
            // Check if already a member
            var existingGroup = await this.groupService.GetGroupAsync(groupId);
            if (existingGroup != null)
            {
                this.loggingService.LogInfo($"Already a member of group {groupId}");
                return true;
            }

            // Find the Drive file for this group
            var groupFiles = await this.googleDriveService.ListGroupFilesAsync();
            var groupFile = groupFiles.FirstOrDefault(f => f.GroupId == groupId);
            
            if (groupFile.FileId == null)
            {
                this.loggingService.LogError($"No Drive file found for group {groupId}");
                return false;
            }

            // Note: The actual sync will be handled by GoogleSyncService
            // This method just validates the invitation is valid
            this.loggingService.LogInfo($"Invitation accepted for group {groupId}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Error accepting invitation: {ex.Message}");
            return false;
        }
    }

    private string CreateInvitationEmailBody(string groupName, string inviterEmail, Guid groupId)
    {
        var deepLink = $"costsharingapp://join?groupId={groupId}";
        
        var body = new StringBuilder();
        body.AppendLine("<!DOCTYPE html>");
        body.AppendLine("<html>");
        body.AppendLine("<head><meta charset=\"UTF-8\"></head>");
        body.AppendLine("<body style=\"font-family: Arial, sans-serif; line-height: 1.6; color: #333;\">");
        body.AppendLine("<div style=\"max-width: 600px; margin: 0 auto; padding: 20px;\">");
        body.AppendLine($"<h2 style=\"color: #4CAF50;\">You're invited to join \"{groupName}\"</h2>");
        body.AppendLine($"<p>Hi there!</p>");
        body.AppendLine($"<p><strong>{inviterEmail}</strong> has invited you to join the group <strong>\"{groupName}\"</strong> on Cost Sharing App.</p>");
        body.AppendLine("<p>Cost Sharing App helps you:</p>");
        body.AppendLine("<ul>");
        body.AppendLine("<li>Track shared expenses with friends</li>");
        body.AppendLine("<li>Split bills fairly</li>");
        body.AppendLine("<li>Settle debts easily</li>");
        body.AppendLine("</ul>");
        body.AppendLine("<div style=\"margin: 30px 0;\">");
        body.AppendLine($"<a href=\"{deepLink}\" style=\"display: inline-block; padding: 15px 30px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;\">Join Group</a>");
        body.AppendLine("</div>");
        body.AppendLine("<p style=\"color: #666; font-size: 14px;\">If the button doesn't work, copy and paste this link into your browser:</p>");
        body.AppendLine($"<p style=\"color: #666; font-size: 14px; word-break: break-all;\">{deepLink}</p>");
        body.AppendLine("<hr style=\"margin: 30px 0; border: none; border-top: 1px solid #ddd;\">");
        body.AppendLine("<p style=\"color: #999; font-size: 12px;\">This invitation was sent via Cost Sharing App. If you don't want to join this group, you can safely ignore this email.</p>");
        body.AppendLine("</div>");
        body.AppendLine("</body>");
        body.AppendLine("</html>");

        return body.ToString();
    }

    private async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var gmailService = this.googleAuthService.GetGmailService();
            
            var message = new Message
            {
                Raw = this.EncodeEmail(to, subject, htmlBody),
            };

            var request = gmailService.Users.Messages.Send(message, "me");
            await request.ExecuteAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to send email: {ex.Message}");
            return false;
        }
    }

    private string EncodeEmail(string to, string subject, string htmlBody)
    {
        var from = this.googleAuthService.CurrentUserEmail ?? "noreply@costsharingapp.com";
        
        var emailMessage = new StringBuilder();
        emailMessage.AppendLine($"From: {from}");
        emailMessage.AppendLine($"To: {to}");
        emailMessage.AppendLine($"Subject: {subject}");
        emailMessage.AppendLine("MIME-Version: 1.0");
        emailMessage.AppendLine("Content-Type: text/html; charset=UTF-8");
        emailMessage.AppendLine();
        emailMessage.AppendLine(htmlBody);

        var bytes = Encoding.UTF8.GetBytes(emailMessage.ToString());
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", string.Empty);
    }
}
