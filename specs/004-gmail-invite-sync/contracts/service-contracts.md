# Service Contracts: Gmail Invitation & Member Sync

**Feature**: 004-gmail-invite-sync  
**Date**: 2026-01-22  
**Status**: Complete

## Overview

This document defines the service interfaces and method contracts for the invitation feature. Since this is a local-first mobile app (not a REST API), contracts are defined as C# interfaces.

---

## IInvitationLinkingService

New service for managing invitation lifecycle and email-based membership linking.

### Interface Definition

```csharp
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
```

### InvitationResult Record

```csharp
namespace CostSharing.Core.Models;

/// <summary>
/// Result of an invitation operation.
/// </summary>
public record InvitationResult(
    bool Success,
    InvitationType Type,
    string Message,
    Guid? MemberOrInvitationId = null);

/// <summary>
/// Type of invitation result.
/// </summary>
public enum InvitationType
{
    /// <summary>User exists, GroupMember created.</summary>
    DirectMember,
    
    /// <summary>User doesn't exist, PendingInvitation created.</summary>
    PendingInvitation,
    
    /// <summary>Error occurred.</summary>
    Error
}
```

---

## IGmailInvitationService (Existing)

Already defined in `CostSharing.Core/Interfaces/IGmailInvitationService.cs`. No changes needed.

### Usage in This Feature

```csharp
// Called by InvitationLinkingService when sendEmail = true
var (success, error) = await _gmailService.SendInvitationAsync(
    recipientEmail: invitedEmail,
    recipientName: invitedEmail.Split('@')[0], // Use email prefix as name
    groupName: group.Name,
    inviterName: inviter.Name,
    userId: inviterUserId,
    cancellationToken);
```

---

## IAuthService (Modified)

Existing interface needs extension to call invitation linking.

### New Behavior

After `RegisterAsync` and `LoginAsync` return successfully, they should:

```csharp
// In AuthService.RegisterAsync (after creating user):
await _invitationLinkingService.LinkPendingInvitationsAsync(user.Id, user.Email);

// In AuthService.LoginAsync (after authentication):
await _invitationLinkingService.LinkPendingInvitationsAsync(user.Id, user.Email);
```

---

## ICacheService (Existing)

Used for CRUD operations on `PendingInvitation` entity. No interface changes needed.

### Usage

```csharp
// Save invitation
await _cacheService.SaveAsync(pendingInvitation);

// Query by email
var invitations = await _cacheService.GetAllAsync<PendingInvitation>();
var matching = invitations.Where(i => 
    i.InvitedEmail == normalizedEmail && 
    i.Status == InvitationStatus.Pending);
```

---

## Error Codes

| Code | Message | When |
|------|---------|------|
| `already_member` | "{email} is already a member of this group" | Duplicate member check |
| `already_pending` | "An invitation has already been sent to {email}" | Duplicate pending check |
| `not_authorized` | "Only group admins can invite members" | Non-admin tries to invite |
| `group_not_found` | "Group not found" | Invalid groupId |
| `invalid_email` | "Please enter a valid email address" | Email validation failed |
| `gmail_not_authorized` | "Please authorize Gmail to send invitations" | Gmail OAuth not complete |
| `gmail_send_failed` | "Could not send invitation email. Member was added." | Gmail API error (non-blocking) |

---

## Sequence Diagram: Invite Flow

```
┌─────────┐     ┌───────────────┐     ┌────────────┐     ┌─────────┐     ┌───────────────┐
│ UI/VM   │     │ InvitationSvc │     │ CacheSvc   │     │ AuthSvc │     │ GmailSvc      │
└────┬────┘     └───────┬───────┘     └─────┬──────┘     └────┬────┘     └───────┬───────┘
     │                  │                   │                 │                   │
     │ InviteToGroup    │                   │                 │                   │
     │─────────────────►│                   │                 │                   │
     │                  │                   │                 │                   │
     │                  │ GetMembership     │                 │                   │
     │                  │ (check admin)     │                 │                   │
     │                  │──────────────────►│                 │                   │
     │                  │◄──────────────────│                 │                   │
     │                  │                   │                 │                   │
     │                  │ CheckDuplicate    │                 │                   │
     │                  │──────────────────►│                 │                   │
     │                  │◄──────────────────│                 │                   │
     │                  │                   │                 │                   │
     │                  │ FindUserByEmail   │                 │                   │
     │                  │──────────────────────────────────►  │                   │
     │                  │◄──────────────────────────────────  │                   │
     │                  │                   │                 │                   │
     │                  │ [User exists]     │                 │                   │
     │                  │ SaveGroupMember   │                 │                   │
     │                  │──────────────────►│                 │                   │
     │                  │                   │                 │                   │
     │                  │ [User not exists] │                 │                   │
     │                  │ SavePending       │                 │                   │
     │                  │──────────────────►│                 │                   │
     │                  │                   │                 │                   │
     │                  │ SendInvitation    │                 │                   │
     │                  │────────────────────────────────────────────────────────►│
     │                  │◄────────────────────────────────────────────────────────│
     │                  │                   │                 │                   │
     │◄─────────────────│                   │                 │                   │
     │  InvitationResult│                   │                 │                   │
     │                  │                   │                 │                   │
```

---

## Sequence Diagram: Signup/Login Linking

```
┌─────────┐     ┌───────────┐     ┌───────────────┐     ┌────────────┐
│ AuthVM  │     │ AuthSvc   │     │ InvitationSvc │     │ CacheSvc   │
└────┬────┘     └─────┬─────┘     └───────┬───────┘     └─────┬──────┘
     │                │                   │                   │
     │ Register/Login │                   │                   │
     │───────────────►│                   │                   │
     │                │                   │                   │
     │                │ [Auth succeeds]   │                   │
     │                │                   │                   │
     │                │ LinkPending       │                   │
     │                │──────────────────►│                   │
     │                │                   │                   │
     │                │                   │ GetPending(email) │
     │                │                   │──────────────────►│
     │                │                   │◄──────────────────│
     │                │                   │                   │
     │                │                   │ [foreach pending] │
     │                │                   │ CreateGroupMember │
     │                │                   │──────────────────►│
     │                │                   │ UpdateStatus      │
     │                │                   │──────────────────►│
     │                │                   │                   │
     │                │◄──────────────────│                   │
     │                │   linkedCount     │                   │
     │◄───────────────│                   │                   │
     │   success      │                   │                   │
     │                │                   │                   │
     │ [Navigate to dashboard - sees all linked groups]       │
     │                │                   │                   │
```
