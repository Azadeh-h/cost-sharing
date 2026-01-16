// <copyright file="GmailInvitationService.cs" company="CostSharing">
// Copyright (c) CostSharing. All rights reserved.
// </copyright>

using System.Text;
using CostSharing.Core.Interfaces;
using CostSharing.Core.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;

namespace CostSharingApp.Services;

/// <summary>
/// Service for sending group invitations via Gmail API.
/// </summary>
public class GmailInvitationService : IGmailInvitationService
{
    private readonly IDriveAuthService driveAuthService;
    private readonly ILoggingService loggingService;
    private GmailService? gmailService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GmailInvitationService"/> class.
    /// </summary>
    /// <param name="driveAuthService">Drive auth service (reused for OAuth tokens).</param>
    /// <param name="loggingService">Logging service.</param>
    public GmailInvitationService(
        IDriveAuthService driveAuthService,
        ILoggingService loggingService)
    {
        this.driveAuthService = driveAuthService;
        this.loggingService = loggingService;
    }

    /// <inheritdoc/>
    public async Task<bool> IsGmailAuthorizedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await this.driveAuthService.GetAccessTokenAsync(userId, cancellationToken);
            return !string.IsNullOrEmpty(accessToken);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? ErrorMessage)> SendInvitationAsync(
        string recipientEmail,
        string recipientName,
        string groupName,
        string inviterName,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            this.loggingService.LogInfo($"Sending Gmail invitation to {recipientEmail} for group {groupName}");

            await this.InitializeGmailServiceAsync(userId, cancellationToken);

            if (this.gmailService == null)
            {
                this.loggingService.LogError("Gmail service is null after initialization", null);
                return (false, "Gmail service not initialized. Please authorize Google in Settings.");
            }

            this.loggingService.LogInfo("Gmail service initialized, getting user profile...");

            // Get the user's email address (the sender)
            string senderEmail;
            try
            {
                var profile = await this.gmailService.Users.GetProfile("me").ExecuteAsync(cancellationToken);
                senderEmail = profile.EmailAddress;
                this.loggingService.LogInfo($"Got sender email: {senderEmail}");
            }
            catch (Google.GoogleApiException ex)
            {
                this.loggingService.LogError($"Gmail API error: Status={ex.HttpStatusCode}, Error={ex.Error?.Message ?? "unknown"}", ex);
                
                if (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    // Check if it's a scope issue or API not enabled
                    var errorDetail = ex.Error?.Message ?? ex.Message;
                    if (errorDetail.Contains("Gmail API has not been used") || errorDetail.Contains("accessNotConfigured"))
                    {
                        return (false, "Gmail API is not enabled. Please enable it in Google Cloud Console: APIs & Services → Library → Gmail API → Enable");
                    }
                    
                    return (false, $"Gmail access denied. Error: {errorDetail}\n\nMake sure:\n1. Gmail API is enabled in Google Cloud Console\n2. gmail.send scope is added to OAuth consent screen\n3. You revoked and re-authorized in Settings");
                }
                
                return (false, $"Gmail API error ({ex.HttpStatusCode}): {ex.Error?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                this.loggingService.LogError($"Failed to get Gmail profile: {ex.Message}", ex);
                return (false, $"Failed to access Gmail profile: {ex.Message}");
            }

            // Create the email content
            var emailHtml = this.CreateInvitationEmailHtml(recipientName, groupName, inviterName);
            var emailPlainText = this.CreateInvitationEmailPlainText(recipientName, groupName, inviterName);

            this.loggingService.LogInfo($"Creating MIME message from {senderEmail} to {recipientEmail}");

            // Create MIME message
            var mimeMessage = this.CreateMimeMessage(
                senderEmail,
                recipientEmail,
                $"You're invited to join \"{groupName}\" on Cost Sharing App!",
                emailPlainText,
                emailHtml);

            // Encode to base64url
            var rawMessage = Base64UrlEncode(mimeMessage);

            this.loggingService.LogInfo("Sending email via Gmail API...");

            // Send the message
            var message = new Message { Raw = rawMessage };
            try
            {
                await this.gmailService.Users.Messages.Send(message, "me").ExecuteAsync(cancellationToken);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                this.loggingService.LogError($"Gmail send permission denied", ex);
                return (false, "Gmail send permission denied. Please go to Settings, tap 'Revoke Authorization', then re-authorize to grant email permission.");
            }

            this.loggingService.LogInfo($"Successfully sent Gmail invitation to {recipientEmail}");
            return (true, null);
        }
        catch (Google.GoogleApiException ex)
        {
            this.loggingService.LogError($"Gmail API error: {ex.HttpStatusCode} - {ex.Message}", ex);
            return (false, $"Gmail error: {ex.Message}\n\nPlease ensure Gmail API is enabled in Google Cloud Console and you've re-authorized after adding the Gmail scope.");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to send Gmail invitation to {recipientEmail}", ex);
            return (false, $"Failed to send invitation: {ex.Message}");
        }
    }

    /// <summary>
    /// Initializes the Gmail service with OAuth credentials.
    /// </summary>
    private async Task InitializeGmailServiceAsync(Guid userId, CancellationToken cancellationToken)
    {
        this.loggingService.LogInfo($"Initializing Gmail service for user {userId}");

        var accessToken = await this.driveAuthService.GetAccessTokenAsync(userId, cancellationToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            this.loggingService.LogError("No access token available for Gmail", null);
            throw new InvalidOperationException("User not authorized for Gmail access. Please authorize in Settings.");
        }

        this.loggingService.LogInfo($"Got access token, creating Gmail service (token length: {accessToken.Length})");

        var credential = GoogleCredential.FromAccessToken(accessToken);

        this.gmailService = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CostSharingApp",
        });

        this.loggingService.LogInfo("Gmail service created successfully");
    }

    /// <summary>
    /// Creates the HTML version of the invitation email.
    /// </summary>
    private string CreateInvitationEmailHtml(string recipientName, string groupName, string inviterName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        h1 {{ margin: 0; font-size: 24px; }}
        .emoji {{ font-size: 48px; }}
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""emoji"">💰</div>
        <h1>Cost Sharing App</h1>
    </div>
    <div class=""content"">
        <p>Hi {recipientName}!</p>
        <p><strong>{inviterName}</strong> has invited you to join the group <strong>""{groupName}""</strong> on Cost Sharing App.</p>
        <p>Cost Sharing App makes it easy to:</p>
        <ul>
            <li>✅ Track shared expenses with friends and family</li>
            <li>✅ Split bills evenly or with custom amounts</li>
            <li>✅ See who owes who at a glance</li>
            <li>✅ Sync across devices with Google Drive</li>
        </ul>
        <p><strong>To get started:</strong></p>
        <ol>
            <li>Download the Cost Sharing App (APK shared by {inviterName})</li>
            <li>Open the app and it will create your account automatically</li>
            <li>Go to Settings and authorize Google Drive</li>
            <li>Your group data will sync automatically!</li>
        </ol>
        <p>That's it! Once you've authorized Google Drive, all the expenses from <strong>""{groupName}""</strong> will appear on your device.</p>
    </div>
    <div class=""footer"">
        <p>This invitation was sent by {inviterName} via Cost Sharing App.</p>
        <p>If you didn't expect this email, you can safely ignore it.</p>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Creates the plain text version of the invitation email.
    /// </summary>
    private string CreateInvitationEmailPlainText(string recipientName, string groupName, string inviterName)
    {
        return $@"Hi {recipientName}!

{inviterName} has invited you to join the group ""{groupName}"" on Cost Sharing App.

Cost Sharing App makes it easy to:
- Track shared expenses with friends and family
- Split bills evenly or with custom amounts
- See who owes who at a glance
- Sync across devices with Google Drive

To get started:
1. Download the Cost Sharing App (APK shared by {inviterName})
2. Open the app and it will create your account automatically
3. Go to Settings and authorize Google Drive
4. Your group data will sync automatically!

That's it! Once you've authorized Google Drive, all the expenses from ""{groupName}"" will appear on your device.

---
This invitation was sent by {inviterName} via Cost Sharing App.
If you didn't expect this email, you can safely ignore it.";
    }

    /// <summary>
    /// Creates a MIME message string.
    /// </summary>
    private string CreateMimeMessage(string from, string to, string subject, string plainText, string html)
    {
        var boundary = $"boundary_{Guid.NewGuid():N}";

        var sb = new StringBuilder();
        sb.AppendLine($"From: {from}");
        sb.AppendLine($"To: {to}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine("MIME-Version: 1.0");
        sb.AppendLine($"Content-Type: multipart/alternative; boundary=\"{boundary}\"");
        sb.AppendLine();

        // Plain text part
        sb.AppendLine($"--{boundary}");
        sb.AppendLine("Content-Type: text/plain; charset=utf-8");
        sb.AppendLine("Content-Transfer-Encoding: quoted-printable");
        sb.AppendLine();
        sb.AppendLine(plainText);
        sb.AppendLine();

        // HTML part
        sb.AppendLine($"--{boundary}");
        sb.AppendLine("Content-Type: text/html; charset=utf-8");
        sb.AppendLine("Content-Transfer-Encoding: quoted-printable");
        sb.AppendLine();
        sb.AppendLine(html);
        sb.AppendLine();

        sb.AppendLine($"--{boundary}--");

        return sb.ToString();
    }

    /// <summary>
    /// Encodes a string to base64url format required by Gmail API.
    /// </summary>
    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var base64 = Convert.ToBase64String(bytes);

        // Convert to base64url
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", string.Empty);
    }
}
