// <copyright file="InvitationLinkingServiceTests.cs" company="CostSharing">
// Copyright (c) CostSharing. All rights reserved.
// </copyright>

using CostSharing.Core.Models;

namespace CostSharingApp.Tests.Services;

/// <summary>
/// Unit tests for invitation logic - email validation, normalization, and business rules.
/// These tests verify the core invitation logic without MAUI dependencies.
/// </summary>
public class InvitationLinkingServiceTests
{
    private readonly Guid testGroupId;
    private readonly Guid testUserId;
    private readonly Guid testInviterId;

    public InvitationLinkingServiceTests()
    {
        this.testGroupId = Guid.NewGuid();
        this.testUserId = Guid.NewGuid();
        this.testInviterId = Guid.NewGuid();
    }

    #region Email Validation Tests

    [Theory]
    [InlineData("valid@example.com", true)]
    [InlineData("user.name@domain.org", true)]
    [InlineData("test+tag@gmail.com", true)]
    [InlineData("user_name@sub.domain.com", true)]
    [InlineData("123@numbers.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@nodomain.com", false)]
    [InlineData("noatsign.com", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("spaces in@email.com", false)]
    [InlineData("double@@at.com", false)]
    public void IsValidEmail_VariousInputs_ReturnsExpected(string email, bool expected)
    {
        // Act
        var result = IsValidEmail(email);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Email Normalization Tests

    [Fact]
    public void NormalizeEmail_UppercaseEmail_ReturnsLowercase()
    {
        // Arrange
        var email = "TEST@EXAMPLE.COM";

        // Act
        var result = NormalizeEmail(email);

        // Assert
        Assert.Equal("test@example.com", result);
    }

    [Fact]
    public void NormalizeEmail_MixedCaseEmail_ReturnsLowercase()
    {
        // Arrange
        var email = "John.Doe@Example.COM";

        // Act
        var result = NormalizeEmail(email);

        // Assert
        Assert.Equal("john.doe@example.com", result);
    }

    [Fact]
    public void NormalizeEmail_EmailWithSpaces_TrimsAndLowercases()
    {
        // Arrange
        var email = "  user@example.com  ";

        // Act
        var result = NormalizeEmail(email);

        // Assert
        Assert.Equal("user@example.com", result);
    }

    [Theory]
    [InlineData("USER@GMAIL.COM", "user@gmail.com")]
    [InlineData("User.Name@Domain.ORG", "user.name@domain.org")]
    [InlineData("  TRIMME@TEST.COM  ", "trimme@test.com")]
    public void NormalizeEmail_VariousInputs_NormalizesCorrectly(string input, string expected)
    {
        // Act
        var result = NormalizeEmail(input);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Invitation Status Tests

    [Fact]
    public void InvitationStatus_PendingIsDefault()
    {
        // Arrange
        var invitation = new PendingInvitation
        {
            Id = Guid.NewGuid(),
            GroupId = this.testGroupId,
            InvitedEmail = "test@example.com",
            InvitedByUserId = this.testInviterId,
            InvitedAt = DateTime.UtcNow,
            Status = InvitationStatus.Pending,
        };

        // Assert
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }

    [Fact]
    public void InvitationStatus_CanBeSetToAccepted()
    {
        // Arrange
        var invitation = new PendingInvitation
        {
            Id = Guid.NewGuid(),
            GroupId = this.testGroupId,
            InvitedEmail = "test@example.com",
            Status = InvitationStatus.Pending,
        };

        // Act
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.LinkedUserId = this.testUserId;

        // Assert
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.NotNull(invitation.AcceptedAt);
        Assert.Equal(this.testUserId, invitation.LinkedUserId);
    }

    [Fact]
    public void InvitationStatus_CanBeSetToCancelled()
    {
        // Arrange
        var invitation = new PendingInvitation
        {
            Id = Guid.NewGuid(),
            GroupId = this.testGroupId,
            InvitedEmail = "test@example.com",
            Status = InvitationStatus.Pending,
        };

        // Act
        invitation.Status = InvitationStatus.Cancelled;

        // Assert
        Assert.Equal(InvitationStatus.Cancelled, invitation.Status);
    }

    #endregion

    #region InvitationResult Tests

    [Fact]
    public void InvitationResult_SuccessfulDirectMember_HasCorrectProperties()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        // Act
        var result = new InvitationResult(
            true,
            InvitationType.DirectMember,
            "user@example.com has been added to the group",
            memberId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InvitationType.DirectMember, result.Type);
        Assert.Contains("has been added", result.Message);
        Assert.Equal(memberId, result.MemberOrInvitationId);
    }

    [Fact]
    public void InvitationResult_SuccessfulPendingInvitation_HasCorrectProperties()
    {
        // Arrange
        var invitationId = Guid.NewGuid();

        // Act
        var result = new InvitationResult(
            true,
            InvitationType.PendingInvitation,
            "Invitation sent to newuser@example.com",
            invitationId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InvitationType.PendingInvitation, result.Type);
        Assert.Contains("Invitation sent", result.Message);
        Assert.Equal(invitationId, result.MemberOrInvitationId);
    }

    [Fact]
    public void InvitationResult_Error_HasCorrectProperties()
    {
        // Act
        var result = new InvitationResult(
            false,
            InvitationType.Error,
            "Group not found");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(InvitationType.Error, result.Type);
        Assert.Equal("Group not found", result.Message);
        Assert.Null(result.MemberOrInvitationId);
    }

    [Theory]
    [InlineData("Please enter a valid email address")]
    [InlineData("Group not found")]
    [InlineData("Only group admins can invite members")]
    [InlineData("user@test.com is already a member of this group")]
    public void InvitationResult_ErrorMessages_AreUserFriendly(string errorMessage)
    {
        // Act
        var result = new InvitationResult(false, InvitationType.Error, errorMessage);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Message);
        Assert.DoesNotContain("Exception", result.Message);
        Assert.DoesNotContain("null", result.Message.ToLower());
    }

    #endregion

    #region GroupMember Tests

    [Fact]
    public void GroupMember_NewMember_HasCorrectRole()
    {
        // Arrange & Act
        var member = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = this.testGroupId,
            UserId = this.testUserId,
            Role = GroupRole.Member,
            JoinedAt = DateTime.UtcNow,
            AddedBy = this.testInviterId,
        };

        // Assert
        Assert.Equal(GroupRole.Member, member.Role);
        Assert.NotEqual(Guid.Empty, member.Id);
        Assert.Equal(this.testGroupId, member.GroupId);
        Assert.Equal(this.testUserId, member.UserId);
        Assert.Equal(this.testInviterId, member.AddedBy);
    }

    [Fact]
    public void GroupMember_AdminRole_IsDistinct()
    {
        // Arrange
        var adminMember = new GroupMember { Role = GroupRole.Admin };
        var regularMember = new GroupMember { Role = GroupRole.Member };

        // Assert
        Assert.NotEqual(adminMember.Role, regularMember.Role);
        Assert.Equal(GroupRole.Admin, adminMember.Role);
        Assert.Equal(GroupRole.Member, regularMember.Role);
    }

    #endregion

    #region Duplicate Detection Tests

    [Fact]
    public void DuplicateCheck_SameEmailDifferentCase_ShouldMatch()
    {
        // Arrange
        var existingEmail = "user@example.com";
        var newEmail = "USER@EXAMPLE.COM";

        // Act
        var normalizedExisting = NormalizeEmail(existingEmail);
        var normalizedNew = NormalizeEmail(newEmail);

        // Assert
        Assert.Equal(normalizedExisting, normalizedNew);
    }

    [Fact]
    public void DuplicateCheck_EmailWithSpaces_ShouldMatchTrimmed()
    {
        // Arrange
        var existingEmail = "user@example.com";
        var newEmailWithSpaces = "  user@example.com  ";

        // Act
        var normalizedExisting = NormalizeEmail(existingEmail);
        var normalizedNew = NormalizeEmail(newEmailWithSpaces);

        // Assert
        Assert.Equal(normalizedExisting, normalizedNew);
    }

    #endregion

    #region Helper Methods (Copied from InvitationLinkingService for testing)

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    #endregion
}
