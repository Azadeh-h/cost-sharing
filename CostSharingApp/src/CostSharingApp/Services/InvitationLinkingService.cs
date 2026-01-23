// <copyright file="InvitationLinkingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using CostSharing.Core.Interfaces;
using CostSharing.Core.Models;

namespace CostSharingApp.Services;

/// <summary>
/// Service for managing group invitations and linking memberships.
/// </summary>
public partial class InvitationLinkingService : IInvitationLinkingService
{
    private readonly ICacheService cacheService;
    private readonly ILoggingService loggingService;
    private readonly IAuthService authService;
    private readonly IGmailInvitationService gmailInvitationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationLinkingService"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service for data access.</param>
    /// <param name="loggingService">Logging service.</param>
    /// <param name="authService">Authentication service for user lookup.</param>
    /// <param name="gmailInvitationService">Gmail service for sending invitations.</param>
    public InvitationLinkingService(
        ICacheService cacheService,
        ILoggingService loggingService,
        IAuthService authService,
        IGmailInvitationService gmailInvitationService)
    {
        this.cacheService = cacheService;
        this.loggingService = loggingService;
        this.authService = authService;
        this.gmailInvitationService = gmailInvitationService;
    }

    /// <inheritdoc/>
    public async Task<InvitationResult> InviteToGroupAsync(
        Guid groupId,
        string invitedEmail,
        Guid inviterUserId,
        bool sendEmail = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Normalize email
            var normalizedEmail = this.NormalizeEmail(invitedEmail);

            // Validate email format
            if (!this.IsValidEmail(normalizedEmail))
            {
                return new InvitationResult(false, InvitationType.Error, "Please enter a valid email address");
            }

            // Get group
            var group = await this.cacheService.GetAsync<CostSharing.Core.Models.Group>(groupId);
            if (group == null)
            {
                return new InvitationResult(false, InvitationType.Error, "Group not found");
            }

            // Check if inviter is admin
            var inviterMembership = await this.GetMembershipAsync(groupId, inviterUserId);
            if (inviterMembership?.Role != GroupRole.Admin)
            {
                return new InvitationResult(false, InvitationType.Error, "Only group admins can invite members");
            }

            // Check for duplicates
            if (await this.IsAlreadyMemberOrPendingAsync(groupId, normalizedEmail, cancellationToken))
            {
                return new InvitationResult(false, InvitationType.Error, $"{normalizedEmail} is already a member of this group");
            }

            // Check if user exists
            var existingUser = await this.FindUserByEmailAsync(normalizedEmail);
            var inviter = await this.authService.GetUserByIdAsync(inviterUserId);

            InvitationResult result;

            if (existingUser != null)
            {
                // User exists - create GroupMember directly
                var member = new GroupMember
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = existingUser.Id,
                    Role = GroupRole.Member,
                    JoinedAt = DateTime.UtcNow,
                    AddedBy = inviterUserId,
                };

                await this.cacheService.SaveAsync(member);
                this.loggingService.LogInfo($"Added existing user {normalizedEmail} to group {group.Name}");

                result = new InvitationResult(true, InvitationType.DirectMember, $"{normalizedEmail} has been added to the group", member.Id);
            }
            else
            {
                // User doesn't exist - create PendingInvitation
                var invitation = new PendingInvitation
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    InvitedEmail = normalizedEmail,
                    InvitedByUserId = inviterUserId,
                    InvitedAt = DateTime.UtcNow,
                    Status = InvitationStatus.Pending,
                };

                await this.cacheService.SaveAsync(invitation);
                this.loggingService.LogInfo($"Created pending invitation for {normalizedEmail} to group {group.Name}");

                result = new InvitationResult(true, InvitationType.PendingInvitation, $"Invitation sent to {normalizedEmail}", invitation.Id);
            }

            // Send email notification (non-blocking)
            if (sendEmail && inviter != null)
            {
                await this.TrySendInvitationEmailAsync(
                    normalizedEmail,
                    group.Name,
                    inviter.Name,
                    inviterUserId,
                    cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to invite user to group", ex);
            return new InvitationResult(false, InvitationType.Error, "An error occurred while sending the invitation");
        }
    }

    /// <inheritdoc/>
    public async Task<int> LinkPendingInvitationsAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedEmail = this.NormalizeEmail(email);

            // Get all pending invitations for this email
            var allInvitations = await this.cacheService.GetAllAsync<PendingInvitation>();
            var pendingInvitations = allInvitations
                .Where(i => i.InvitedEmail == normalizedEmail && i.Status == InvitationStatus.Pending)
                .ToList();

            if (pendingInvitations.Count == 0)
            {
                return 0;
            }

            var linkedCount = 0;

            foreach (var invitation in pendingInvitations)
            {
                try
                {
                    // Create GroupMember
                    var member = new GroupMember
                    {
                        Id = Guid.NewGuid(),
                        GroupId = invitation.GroupId,
                        UserId = userId,
                        Role = GroupRole.Member,
                        JoinedAt = DateTime.UtcNow,
                        AddedBy = invitation.InvitedByUserId,
                    };

                    await this.cacheService.SaveAsync(member);

                    // Update invitation status
                    invitation.Status = InvitationStatus.Accepted;
                    invitation.AcceptedAt = DateTime.UtcNow;
                    invitation.LinkedUserId = userId;
                    await this.cacheService.SaveAsync(invitation);

                    linkedCount++;
                    this.loggingService.LogInfo($"Linked pending invitation for group {invitation.GroupId} to user {userId}");
                }
                catch (Exception ex)
                {
                    this.loggingService.LogError($"Failed to link invitation {invitation.Id}", ex);
                }
            }

            this.loggingService.LogInfo($"Linked {linkedCount} pending invitations for user {userId}");
            return linkedCount;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to link pending invitations", ex);
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<List<PendingInvitation>> GetPendingInvitationsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allInvitations = await this.cacheService.GetAllAsync<PendingInvitation>();
            return allInvitations
                .Where(i => i.GroupId == groupId && i.Status == InvitationStatus.Pending)
                .ToList();
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to get pending invitations", ex);
            return new List<PendingInvitation>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CancelInvitationAsync(
        Guid invitationId,
        Guid cancelledByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invitation = await this.cacheService.GetAsync<PendingInvitation>(invitationId);
            if (invitation == null || invitation.Status != InvitationStatus.Pending)
            {
                return false;
            }

            // Check if user is admin
            var membership = await this.GetMembershipAsync(invitation.GroupId, cancelledByUserId);
            if (membership?.Role != GroupRole.Admin)
            {
                this.loggingService.LogWarning($"Non-admin user {cancelledByUserId} tried to cancel invitation");
                return false;
            }

            invitation.Status = InvitationStatus.Cancelled;
            await this.cacheService.SaveAsync(invitation);

            this.loggingService.LogInfo($"Invitation {invitationId} cancelled by user {cancelledByUserId}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to cancel invitation", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAlreadyMemberOrPendingAsync(
        Guid groupId,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedEmail = this.NormalizeEmail(email);

            // Check if user exists and is already a member
            var existingUser = await this.FindUserByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                var membership = await this.GetMembershipAsync(groupId, existingUser.Id);
                if (membership != null)
                {
                    return true;
                }
            }

            // Check for pending invitations
            var allInvitations = await this.cacheService.GetAllAsync<PendingInvitation>();
            return allInvitations.Any(i =>
                i.GroupId == groupId &&
                i.InvitedEmail == normalizedEmail &&
                i.Status == InvitationStatus.Pending);
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to check member/pending status", ex);
            return false;
        }
    }

    /// <summary>
    /// Normalizes email to lowercase.
    /// </summary>
    /// <param name="email">Email to normalize.</param>
    /// <returns>Normalized email.</returns>
    private string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Validates email format.
    /// </summary>
    /// <param name="email">Email to validate.</param>
    /// <returns>True if valid.</returns>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return EmailRegex().IsMatch(email);
    }

    /// <summary>
    /// Finds a user by email address.
    /// </summary>
    /// <param name="normalizedEmail">Normalized email.</param>
    /// <returns>User or null.</returns>
    private async Task<User?> FindUserByEmailAsync(string normalizedEmail)
    {
        var allUsers = await this.authService.GetAllUsersAsync();
        return allUsers.FirstOrDefault(u => this.NormalizeEmail(u.Email) == normalizedEmail);
    }

    /// <summary>
    /// Gets membership for a user in a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="userId">User ID.</param>
    /// <returns>GroupMember or null.</returns>
    private async Task<GroupMember?> GetMembershipAsync(Guid groupId, Guid userId)
    {
        var allMembers = await this.cacheService.GetAllAsync<GroupMember>();
        return allMembers.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId);
    }

    /// <summary>
    /// Attempts to send invitation email (non-blocking).
    /// </summary>
    private async Task TrySendInvitationEmailAsync(
        string recipientEmail,
        string groupName,
        string inviterName,
        Guid inviterUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check Gmail authorization
            var isAuthorized = await this.gmailInvitationService.IsGmailAuthorizedAsync(inviterUserId, cancellationToken);
            if (!isAuthorized)
            {
                this.loggingService.LogWarning($"Gmail not authorized for user {inviterUserId}, skipping email");
                return;
            }

            // Use email prefix as recipient name
            var recipientName = recipientEmail.Split('@')[0];

            var (success, error) = await this.gmailInvitationService.SendInvitationAsync(
                recipientEmail,
                recipientName,
                groupName,
                inviterName,
                inviterUserId,
                cancellationToken);

            if (success)
            {
                this.loggingService.LogInfo($"Invitation email sent to {recipientEmail}");
            }
            else
            {
                this.loggingService.LogWarning($"Failed to send invitation email to {recipientEmail}: {error}");
            }
        }
        catch (Exception ex)
        {
            // Non-blocking - log and continue
            this.loggingService.LogError($"Error sending invitation email to {recipientEmail}", ex);
        }
    }

    /// <summary>
    /// Regex for email validation.
    /// </summary>
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
