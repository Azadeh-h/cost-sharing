using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CostSharingApp.Services;

/// <summary>
/// Provides email and SMS notification services using SendGrid and Twilio.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILoggingService loggingService;
    private readonly string sendGridApiKey;
    private readonly string sendGridFromEmail;
    private readonly string sendGridFromName;
    private readonly string twilioAccountSid;
    private readonly string twilioAuthToken;
    private readonly string twilioPhoneNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="loggingService">Logging service.</param>
    /// <param name="sendGridApiKey">SendGrid API key.</param>
    /// <param name="sendGridFromEmail">From email address.</param>
    /// <param name="sendGridFromName">From display name.</param>
    /// <param name="twilioAccountSid">Twilio account SID.</param>
    /// <param name="twilioAuthToken">Twilio auth token.</param>
    /// <param name="twilioPhoneNumber">Twilio phone number.</param>
    public NotificationService(
        ILoggingService loggingService,
        string sendGridApiKey,
        string sendGridFromEmail,
        string sendGridFromName,
        string twilioAccountSid,
        string twilioAuthToken,
        string twilioPhoneNumber)
    {
        this.loggingService = loggingService;
        this.sendGridApiKey = sendGridApiKey;
        this.sendGridFromEmail = sendGridFromEmail;
        this.sendGridFromName = sendGridFromName;
        this.twilioAccountSid = twilioAccountSid;
        this.twilioAuthToken = twilioAuthToken;
        this.twilioPhoneNumber = twilioPhoneNumber;
    }

    /// <summary>
    /// Sends an email using SendGrid.
    /// </summary>
    /// <param name="toEmail">Recipient email.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="htmlContent">HTML email content.</param>
    /// <param name="plainTextContent">Plain text fallback.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string plainTextContent)
    {
        try
        {
            var client = new SendGridClient(this.sendGridApiKey);
            var from = new EmailAddress(this.sendGridFromEmail, this.sendGridFromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                this.loggingService.LogInfo($"Email sent to {toEmail}: {subject}");
                return true;
            }
            else
            {
                this.loggingService.LogWarning($"Email send failed: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Email send failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Sends an SMS using Twilio.
    /// </summary>
    /// <param name="toPhone">Recipient phone number (E.164 format).</param>
    /// <param name="message">SMS message (max 160 chars for single SMS).</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SendSmsAsync(string toPhone, string message)
    {
        try
        {
            TwilioClient.Init(this.twilioAccountSid, this.twilioAuthToken);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(this.twilioPhoneNumber),
                to: new PhoneNumber(toPhone));

            if (messageResource.Status == MessageResource.StatusEnum.Sent ||
                messageResource.Status == MessageResource.StatusEnum.Queued ||
                messageResource.Status == MessageResource.StatusEnum.Delivered)
            {
                this.loggingService.LogInfo($"SMS sent to {toPhone}: {messageResource.Sid}");
                return true;
            }
            else
            {
                this.loggingService.LogWarning($"SMS send failed: {messageResource.Status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("SMS send failed", ex);
            return false;
        }
    }
}

/// <summary>
/// Interface for notification service.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends an email.
    /// </summary>
    /// <param name="toEmail">Recipient email.</param>
    /// <param name="subject">Subject.</param>
    /// <param name="htmlContent">HTML content.</param>
    /// <param name="plainTextContent">Plain text.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string plainTextContent);

    /// <summary>
    /// Sends an SMS.
    /// </summary>
    /// <param name="toPhone">Recipient phone.</param>
    /// <param name="message">Message.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SendSmsAsync(string toPhone, string message);
}
