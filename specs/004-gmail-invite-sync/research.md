# Research: Gmail Invitation & Member Sync

**Feature**: 004-gmail-invite-sync  
**Date**: 2026-01-22  
**Status**: Complete

## Research Summary

All technical unknowns from the Technical Context have been resolved. This document captures decisions, rationale, and alternatives considered.

---

## 1. Email Matching Strategy

### Decision
Use case-insensitive email comparison by normalizing emails to lowercase before storage and comparison.

### Rationale
- RFC 5321 specifies the local part (before @) is case-sensitive, but in practice all major email providers treat emails as case-insensitive
- User expectation is that `John@Gmail.com` and `john@gmail.com` are the same
- Prevents accidental duplicate accounts and missed invitation links

### Implementation
```csharp
// Normalize on save and compare
email = email.Trim().ToLowerInvariant();
```

### Alternatives Considered
1. **Store as-is, compare case-insensitive**: Rejected - leads to UI inconsistencies and storage of duplicates
2. **Respect RFC case-sensitivity**: Rejected - no user expects this behavior

---

## 2. Pending Invitation Entity Design

### Decision
Create a `PendingInvitation` entity that stores invitations to emails that don't have accounts yet.

### Rationale
- Existing `GroupMember` requires a `UserId` which doesn't exist for unregistered users
- Separating pending from active allows clear status tracking
- On signup, link pending invitations by email, then create real `GroupMember` records

### Schema
```
PendingInvitation:
  - Id: Guid (PK)
  - GroupId: Guid (FK)
  - InvitedEmail: string (normalized)
  - InvitedByUserId: Guid
  - InvitedAt: DateTime
  - Status: enum (Pending, Accepted, Expired)
```

### Alternatives Considered
1. **Placeholder User records**: Rejected - pollutes User table with non-real users
2. **Nullable UserId in GroupMember**: Rejected - violates data integrity; makes queries complex

---

## 3. Invitation Email Best Practices (Gmail API)

### Decision
Use existing `IGmailInvitationService.SendInvitationAsync` with graceful error handling.

### Rationale
- Interface already exists with proper parameters (recipientEmail, recipientName, groupName, inviterName)
- Non-blocking approach (member added even if email fails) prevents user frustration
- Gmail OAuth flow already implemented in the app

### Best Practices Applied
1. **Retry with exponential backoff** for transient failures (rate limits)
2. **Queue for offline** - store failed invitations to retry when online
3. **User feedback** - show toast for success/failure but don't block flow

### Alternatives Considered
1. **SendGrid/SMTP**: Rejected - Gmail already integrated; user controls their own email
2. **Blocking on email success**: Rejected - email delivery is unreliable; UX suffers

---

## 4. Duplicate Prevention Strategy

### Decision
Unique constraint on `(GroupId, InvitedEmail)` for `PendingInvitation` and `(GroupId, UserId)` for `GroupMember`.

### Rationale
- Database-level constraint prevents race conditions
- Check before insert provides user-friendly error message
- Two-layer approach handles both pending and active members

### Implementation
```csharp
// Check before adding
var existingMember = await GetMemberByEmailAsync(groupId, email);
var existingPending = await GetPendingByEmailAsync(groupId, email);
if (existingMember != null || existingPending != null)
    return ("already_member", $"{email} is already a member of this group");
```

### Alternatives Considered
1. **Application-level check only**: Rejected - race conditions possible
2. **Allow duplicates, filter in UI**: Rejected - corrupts expense calculations

---

## 5. Invitation Linking on Sign-In

### Decision
During registration or sign-in, check for pending invitations matching the user's email and auto-link.

### Rationale
- Seamless user experience - no extra steps needed
- Matches user expectation ("I was invited, I sign in, I see my groups")
- Existing `AuthService.RegisterAsync` and `LoginAsync` can call linking service

### Flow
```
1. User signs up/in with email X
2. Query PendingInvitation WHERE InvitedEmail = X AND Status = Pending
3. For each match:
   a. Create GroupMember (GroupId, UserId, Role=Member)
   b. Update PendingInvitation.Status = Accepted
4. User sees their groups immediately
```

### Alternatives Considered
1. **Manual "accept invitation" step**: Rejected - unnecessary friction
2. **Email verification required first**: Rejected - adds complexity; email was already verified by receiving invitation

---

## 6. Admin Permission Check

### Decision
Only group admins (Role = Admin) can add members. Check at service layer before adding.

### Rationale
- Existing `GroupRole.Admin` enum already exists
- Prevents unauthorized member additions
- Consistent with typical group permission models

### Implementation
```csharp
var inviterMembership = await GetMembershipAsync(groupId, inviterUserId);
if (inviterMembership?.Role != GroupRole.Admin)
    return ("not_authorized", "Only group admins can add members");
```

---

## 7. Google Drive Sync for Invitations

### Decision
Sync `PendingInvitation` records via Google Drive alongside existing entity sync.

### Rationale
- When group creator invites someone, the invitation should be available when invitee signs in (potentially on different device)
- Existing Drive sync pattern can be extended
- Offline queue (`IOfflineQueueService`) handles queuing

### Sync Strategy
- Include `PendingInvitation` in Drive backup JSON
- On sync, merge by `Id` (server wins for conflicts)
- Linking happens locally on sign-in

---

## Dependencies Checklist

| Dependency | Version | Status | Notes |
|------------|---------|--------|-------|
| Google.Apis.Gmail.v1 | 1.73.0.3987 | ✅ Installed | Send emails |
| sqlite-net-pcl | 1.9.172 | ✅ Installed | PendingInvitation storage |
| CommunityToolkit.Mvvm | 8.4.0 | ✅ Installed | ViewModel bindings |
| IGmailInvitationService | - | ✅ Exists | Interface ready |
| GroupMember model | - | ✅ Exists | Junction table ready |

---

## Open Questions Resolved

| Question | Resolution |
|----------|------------|
| How to handle invitations to non-existent users? | PendingInvitation entity |
| Case-sensitive email matching? | No - normalize to lowercase |
| What if Gmail send fails? | Non-blocking; member still added |
| How to prevent duplicates? | DB constraints + service-level check |
| Who can invite? | Admins only (GroupRole.Admin) |

---

## Next Steps

1. Generate `data-model.md` with PendingInvitation entity definition
2. Create API contracts in `/contracts/`
3. Generate `quickstart.md` for developer onboarding
