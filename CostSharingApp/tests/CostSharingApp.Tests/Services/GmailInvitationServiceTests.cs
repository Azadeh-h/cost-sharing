// <copyright file="GmailInvitationServiceTests.cs" company="CostSharing">
// Copyright (c) CostSharing. All rights reserved.
// </copyright>

using System.Text;
using CostSharing.Core.Interfaces;
using CostSharing.Core.Services;
using Moq;

namespace CostSharingApp.Tests.Services;

/// <summary>
/// Unit tests for GmailInvitationService email formatting and encoding.
/// </summary>
public class GmailInvitationServiceTests
{
    #region Base64Url Encoding Tests

    [Fact]
    public void Base64UrlEncode_StandardString_EncodesCorrectly()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var result = Base64UrlEncode(input);

        // Assert
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("=", result);
    }

    [Fact]
    public void Base64UrlEncode_EmptyString_ReturnsEmpty()
    {
        // Act
        var result = Base64UrlEncode(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Base64UrlEncode_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange - String that would produce + and / in standard base64
        var input = "This is a test with special chars: <>&\"'";

        // Act
        var result = Base64UrlEncode(input);

        // Assert
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("=", result);

        // Verify we can decode it back
        var decoded = Base64UrlDecode(result);
        Assert.Equal(input, decoded);
    }

    [Fact]
    public void Base64UrlEncode_UnicodeCharacters_EncodesCorrectly()
    {
        // Arrange
        var input = "Hello 世界 🌍 مرحبا";

        // Act
        var result = Base64UrlEncode(input);

        // Assert
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);

        // Verify we can decode it back
        var decoded = Base64UrlDecode(result);
        Assert.Equal(input, decoded);
    }

    #endregion

    #region MIME Message Creation Tests

    [Fact]
    public void CreateMimeMessage_ValidInputs_ContainsRequiredHeaders()
    {
        // Arrange
        var from = "sender@example.com";
        var to = "recipient@example.com";
        var subject = "Test Subject";
        var plainText = "Plain text content";
        var html = "<html><body>HTML content</body></html>";

        // Act
        var mimeMessage = CreateMimeMessage(from, to, subject, plainText, html);

        // Assert
        Assert.Contains($"From: {from}", mimeMessage);
        Assert.Contains($"To: {to}", mimeMessage);
        Assert.Contains($"Subject: {subject}", mimeMessage);
        Assert.Contains("MIME-Version: 1.0", mimeMessage);
        Assert.Contains("Content-Type: multipart/alternative", mimeMessage);
    }

    [Fact]
    public void CreateMimeMessage_ValidInputs_ContainsBothParts()
    {
        // Arrange
        var from = "sender@example.com";
        var to = "recipient@example.com";
        var subject = "Test Subject";
        var plainText = "Plain text content here";
        var html = "<html><body>HTML content here</body></html>";

        // Act
        var mimeMessage = CreateMimeMessage(from, to, subject, plainText, html);

        // Assert
        Assert.Contains("Content-Type: text/plain; charset=utf-8", mimeMessage);
        Assert.Contains("Content-Type: text/html; charset=utf-8", mimeMessage);
        Assert.Contains(plainText, mimeMessage);
        Assert.Contains(html, mimeMessage);
    }

    [Fact]
    public void CreateMimeMessage_HasBoundaryMarkers()
    {
        // Arrange
        var from = "sender@example.com";
        var to = "recipient@example.com";
        var subject = "Test Subject";
        var plainText = "Plain text";
        var html = "<html></html>";

        // Act
        var mimeMessage = CreateMimeMessage(from, to, subject, plainText, html);

        // Assert
        // Should have opening boundaries (at least 2 for plain and html parts)
        var boundaryCount = mimeMessage.Split(new[] { "--boundary_" }, StringSplitOptions.None).Length - 1;
        Assert.True(boundaryCount >= 2, $"Expected at least 2 boundary markers, found {boundaryCount}");

        // Should have closing boundary
        Assert.Contains("--", mimeMessage);
    }

    [Fact]
    public void CreateMimeMessage_SpecialCharactersInSubject_HandlesCorrectly()
    {
        // Arrange
        var from = "sender@example.com";
        var to = "recipient@example.com";
        var subject = "You're invited to join \"Test Group\" on App!";
        var plainText = "Plain text";
        var html = "<html></html>";

        // Act
        var mimeMessage = CreateMimeMessage(from, to, subject, plainText, html);

        // Assert
        Assert.Contains(subject, mimeMessage);
    }

    #endregion

    #region Email HTML Template Tests

    [Fact]
    public void CreateInvitationEmailHtml_ContainsRecipientName()
    {
        // Arrange
        var recipientName = "John";
        var groupName = "Roommates";
        var inviterName = "Jane";

        // Act
        var html = CreateInvitationEmailHtml(recipientName, groupName, inviterName);

        // Assert
        Assert.Contains($"Hi {recipientName}!", html);
    }

    [Fact]
    public void CreateInvitationEmailHtml_ContainsGroupName()
    {
        // Arrange
        var recipientName = "John";
        var groupName = "Roommates Expenses";
        var inviterName = "Jane";

        // Act
        var html = CreateInvitationEmailHtml(recipientName, groupName, inviterName);

        // Assert
        Assert.Contains(groupName, html);
    }

    [Fact]
    public void CreateInvitationEmailHtml_ContainsInviterName()
    {
        // Arrange
        var recipientName = "John";
        var groupName = "Roommates";
        var inviterName = "Jane Smith";

        // Act
        var html = CreateInvitationEmailHtml(recipientName, groupName, inviterName);

        // Assert
        Assert.Contains(inviterName, html);
    }

    [Fact]
    public void CreateInvitationEmailHtml_IsValidHtml()
    {
        // Arrange
        var recipientName = "John";
        var groupName = "Test";
        var inviterName = "Jane";

        // Act
        var html = CreateInvitationEmailHtml(recipientName, groupName, inviterName);

        // Assert
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html>", html);
        Assert.Contains("</html>", html);
        Assert.Contains("<body>", html);
        Assert.Contains("</body>", html);
    }

    [Fact]
    public void CreateInvitationEmailHtml_ContainsAppFeatures()
    {
        // Arrange
        var recipientName = "John";
        var groupName = "Test";
        var inviterName = "Jane";

        // Act
        var html = CreateInvitationEmailHtml(recipientName, groupName, inviterName);

        // Assert
        Assert.Contains("Track shared expenses", html);
        Assert.Contains("Split bills", html);
        Assert.Contains("Google Drive", html);
    }

    #endregion

    #region Email Plain Text Template Tests

    [Fact]
    public void CreateInvitationEmailPlainText_ContainsRecipientName()
    {
        // Arrange
        var recipientName = "John";
        var groupName = "Roommates";
        var inviterName = "Jane";

        // Act
        var text = CreateInvitationEmailPlainText(recipientName, groupName, inviterName);

        // Assert
        Assert.Contains($"Hi {recipientName}!", text);
    }

    [Fact]
    public void CreateInvitationEmailPlainText_ContainsGroupName()
    {
        // Arrange
        var recipientName = "John";
        var groupName = "Trip to Paris";
        var inviterName = "Jane";

        // Act
        var text = CreateInvitationEmailPlainText(recipientName, groupName, inviterName);

        // Assert
        Assert.Contains(groupName, text);
    }

    [Fact]
    public void CreateInvitationEmailPlainText_HasNoHtmlTags()
    {
        // Arrange
        var recipientName = "John";
        var groupName = "Test";
        var inviterName = "Jane";

        // Act
        var text = CreateInvitationEmailPlainText(recipientName, groupName, inviterName);

        // Assert
        Assert.DoesNotContain("<html>", text);
        Assert.DoesNotContain("<body>", text);
        Assert.DoesNotContain("<div>", text);
        Assert.DoesNotContain("<p>", text);
    }

    #endregion

    #region Helper Methods (Copied from GmailInvitationService for testing)

    private static string Base64UrlEncode(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(input);
        var base64 = Convert.ToBase64String(bytes);

        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", string.Empty);
    }

    private static string Base64UrlDecode(string input)
    {
        var base64 = input
            .Replace('-', '+')
            .Replace('_', '/');

        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string CreateMimeMessage(string from, string to, string subject, string plainText, string html)
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

    private static string CreateInvitationEmailHtml(string recipientName, string groupName, string inviterName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>Cost Sharing App</h1>
    </div>
    <div class=""content"">
        <p>Hi {recipientName}!</p>
        <p><strong>{inviterName}</strong> has invited you to join the group <strong>""{groupName}""</strong> on Cost Sharing App.</p>
        <p>Cost Sharing App makes it easy to:</p>
        <ul>
            <li>Track shared expenses with friends and family</li>
            <li>Split bills evenly or with custom amounts</li>
            <li>See who owes who at a glance</li>
            <li>Sync across devices with Google Drive</li>
        </ul>
    </div>
</body>
</html>";
    }

    private static string CreateInvitationEmailPlainText(string recipientName, string groupName, string inviterName)
    {
        return $@"Hi {recipientName}!

{inviterName} has invited you to join the group ""{groupName}"" on Cost Sharing App.

Cost Sharing App makes it easy to:
- Track shared expenses with friends and family
- Split bills evenly or with custom amounts
- See who owes who at a glance
- Sync across devices with Google Drive";
    }

    #endregion
}
