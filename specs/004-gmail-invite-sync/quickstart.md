# Quickstart: Gmail Invitation & Member Sync

**Feature**: 004-gmail-invite-sync  
**Date**: 2026-01-22

## Overview

This feature enables group admins to invite members via email using Gmail API. When invited users sign in with the same email, they automatically see the groups they've been invited to.

---

## Prerequisites

Before implementing this feature, ensure:

1. ✅ Gmail API is configured (see `GOOGLE_CONFIG_SETUP.md`)
2. ✅ Email-based authentication is working (Feature 003)
3. ✅ User can sign in/up with email and password
4. ✅ Groups and GroupMembers are functional

---

## Key Concepts

### PendingInvitation
Stores invitations to emails that don't have accounts yet. Located at `CostSharing.Core/Models/PendingInvitation.cs`.

### InvitationLinkingService
Orchestrates the invitation flow - checks for duplicates, creates members or pending invitations, triggers email sending. Located at `CostSharing.Core/Services/InvitationLinkingService.cs`.

### Email Normalization
All emails are stored and compared in lowercase to ensure case-insensitive matching.

---

## Implementation Guide

### Step 1: Add PendingInvitation Model

```csharp
// CostSharing.Core/Models/PendingInvitation.cs
public class PendingInvitation
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;  // Normalized lowercase
    public Guid InvitedByUserId { get; set; }
    public DateTime InvitedAt { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime? AcceptedAt { get; set; }
    public Guid? LinkedUserId { get; set; }
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Cancelled = 3
}
```

### Step 2: Add IInvitationLinkingService Interface

```csharp
// CostSharing.Core/Interfaces/IInvitationLinkingService.cs
public interface IInvitationLinkingService
{
    Task<InvitationResult> InviteToGroupAsync(
        Guid groupId, string email, Guid inviterUserId, 
        bool sendEmail = true, CancellationToken ct = default);
    
    Task<int> LinkPendingInvitationsAsync(
        Guid userId, string email, CancellationToken ct = default);
    
    Task<bool> IsAlreadyMemberOrPendingAsync(
        Guid groupId, string email, CancellationToken ct = default);
}
```

### Step 3: Implement InvitationLinkingService

Key logic in `InviteToGroupAsync`:

```csharp
// 1. Normalize email
var normalizedEmail = email.Trim().ToLowerInvariant();

// 2. Check admin permission
var membership = await GetMembershipAsync(groupId, inviterUserId);
if (membership?.Role != GroupRole.Admin)
    return new InvitationResult(false, InvitationType.Error, "Only admins can invite");

// 3. Check duplicates
if (await IsAlreadyMemberOrPendingAsync(groupId, normalizedEmail))
    return new InvitationResult(false, InvitationType.Error, "Already a member");

// 4. Check if user exists
var existingUser = await FindUserByEmailAsync(normalizedEmail);

if (existingUser != null)
{
    // Create GroupMember directly
    await CreateGroupMemberAsync(groupId, existingUser.Id, inviterUserId);
    await SendInvitationEmailAsync(...); // Non-blocking
    return new InvitationResult(true, InvitationType.DirectMember, "Member added");
}
else
{
    // Create PendingInvitation
    await CreatePendingInvitationAsync(groupId, normalizedEmail, inviterUserId);
    await SendInvitationEmailAsync(...); // Non-blocking
    return new InvitationResult(true, InvitationType.PendingInvitation, "Invitation sent");
}
```

### Step 4: Modify AuthService

Add invitation linking after successful auth:

```csharp
// In RegisterAsync, after saving user:
await _invitationLinkingService.LinkPendingInvitationsAsync(user.Id, user.Email);

// In LoginAsync, after successful auth:
await _invitationLinkingService.LinkPendingInvitationsAsync(user.Id, user.Email);
```

### Step 5: Update GroupMember ViewModel

Add invite by email functionality:

```csharp
[RelayCommand]
private async Task InviteByEmailAsync()
{
    if (string.IsNullOrWhiteSpace(InviteEmail))
        return;
    
    var result = await _invitationService.InviteToGroupAsync(
        GroupId, InviteEmail, CurrentUserId);
    
    if (result.Success)
        await ShowToast(result.Message);
    else
        await ShowError(result.Message);
}
```

---

## Testing

### Unit Tests

```csharp
[Fact]
public async Task InviteToGroup_ExistingUser_CreatesGroupMember()
{
    // Arrange
    var existingUser = new User { Id = Guid.NewGuid(), Email = "test@example.com" };
    // ... setup mocks
    
    // Act
    var result = await _service.InviteToGroupAsync(groupId, "test@example.com", adminId);
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal(InvitationType.DirectMember, result.Type);
}

[Fact]
public async Task LinkPendingInvitations_MatchingEmail_CreatesGroupMembers()
{
    // Arrange
    var pending = new PendingInvitation { GroupId = groupId, InvitedEmail = "new@example.com" };
    // ... setup mocks
    
    // Act
    var linkedCount = await _service.LinkPendingInvitationsAsync(userId, "new@example.com");
    
    // Assert
    Assert.Equal(1, linkedCount);
}
```

### Manual Testing Checklist

- [ ] Invite existing user → They appear in group immediately
- [ ] Invite non-existing email → PendingInvitation created
- [ ] Sign up with invited email → Automatically see groups
- [ ] Try duplicate invite → Error message shown
- [ ] Non-admin tries to invite → Permission error
- [ ] Gmail not authorized → Prompt for authorization
- [ ] Gmail fails → Member still added, error toast shown

---

## Common Issues

### Issue: Invitations not linking on signup

**Cause**: Email case mismatch  
**Fix**: Ensure emails are normalized to lowercase in both `PendingInvitation.InvitedEmail` and `User.Email`

### Issue: Duplicate invitations

**Cause**: Race condition  
**Fix**: Add unique index on `(GroupId, InvitedEmail)` in SQLite

### Issue: Gmail authorization prompt not appearing

**Cause**: OAuth scopes not configured  
**Fix**: Ensure Gmail scope is included in `DriveAuthService` OAuth flow

---

## Files to Modify

| File | Change |
|------|--------|
| `Models/PendingInvitation.cs` | New file |
| `Models/InvitationResult.cs` | New file |
| `Interfaces/IInvitationLinkingService.cs` | New file |
| `Services/InvitationLinkingService.cs` | New file |
| `Services/AuthService.cs` | Call linking after auth |
| `ViewModels/GroupMemberViewModel.cs` | Add invite command |
| `Views/GroupMemberPage.xaml` | Add email input UI |
| `MauiProgram.cs` | Register new services |

---

## Dependencies

- `IGmailInvitationService` - Existing, sends emails
- `ICacheService` - Existing, CRUD for entities
- `IAuthService` - Existing, modified to call linking
- `GroupMember` model - Existing
- `User` model - Existing (email normalization)
