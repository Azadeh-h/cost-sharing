# Data Model: Gmail Invitation & Member Sync

**Feature**: 004-gmail-invite-sync  
**Date**: 2026-01-22  
**Status**: Complete

## Entity Overview

This feature introduces one new entity (`PendingInvitation`) and modifies behavior for existing entities (`User`, `Group`, `GroupMember`).

---

## New Entity: PendingInvitation

Tracks invitations sent to email addresses that do not yet have user accounts.

### Fields

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `Guid` | PK | Unique identifier |
| `GroupId` | `Guid` | FK → Group.Id, NOT NULL | The group the user is invited to |
| `InvitedEmail` | `string` | NOT NULL, max 254 chars | Normalized (lowercase) email address |
| `InvitedByUserId` | `Guid` | FK → User.Id, NOT NULL | Who sent the invitation |
| `InvitedAt` | `DateTime` | NOT NULL | When invitation was created |
| `Status` | `InvitationStatus` | NOT NULL, default Pending | Current status |
| `AcceptedAt` | `DateTime?` | NULL | When invitation was accepted (user signed up) |
| `LinkedUserId` | `Guid?` | FK → User.Id, NULL | User ID after account creation |

### Enum: InvitationStatus

| Value | Int | Description |
|-------|-----|-------------|
| `Pending` | 0 | Awaiting user signup |
| `Accepted` | 1 | User signed up and linked |
| `Expired` | 2 | Invitation no longer valid |
| `Cancelled` | 3 | Inviter revoked invitation |

### Indexes

| Name | Columns | Type | Purpose |
|------|---------|------|---------|
| `IX_PendingInvitation_GroupId_Email` | `GroupId`, `InvitedEmail` | UNIQUE | Prevent duplicate invitations |
| `IX_PendingInvitation_Email` | `InvitedEmail` | NON-UNIQUE | Fast lookup for linking on signup |
| `IX_PendingInvitation_Status` | `Status` | NON-UNIQUE | Filter by status |

### Validation Rules

1. `InvitedEmail` must be a valid email format
2. `InvitedEmail` must be normalized to lowercase before storage
3. `GroupId` must reference an existing group
4. `InvitedByUserId` must be an admin of the referenced group
5. Cannot have duplicate `(GroupId, InvitedEmail)` with `Status = Pending`

### State Transitions

```
                  ┌─────────────┐
                  │   Pending   │
                  └──────┬──────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
   ┌───────────┐   ┌───────────┐   ┌───────────┐
   │  Accepted │   │  Expired  │   │ Cancelled │
   └───────────┘   └───────────┘   └───────────┘
```

- **Pending → Accepted**: User signs up with matching email
- **Pending → Expired**: Time-based expiration (future enhancement)
- **Pending → Cancelled**: Inviter removes invitation

---

## Existing Entity: User

No schema changes. Behavior changes only.

### Email Normalization

- On `RegisterAsync`: Normalize email to lowercase before storing
- On `LoginAsync`: Normalize input email for comparison

### New Behavior

- After registration, call `InvitationLinkingService.LinkPendingInvitationsAsync(userId, email)`
- After login, call `InvitationLinkingService.LinkPendingInvitationsAsync(userId, email)` (in case new invitations arrived)

---

## Existing Entity: Group

No changes.

---

## Existing Entity: GroupMember

No schema changes. Behavior changes only.

### New Behavior

- Before adding member: Check for existing member with same email (prevent duplicates)
- When adding member: If email doesn't have an account, create `PendingInvitation` instead of `GroupMember`

### Validation Rules (Existing)

1. `(GroupId, UserId)` must be unique - enforced at DB level
2. Only admins can add members - enforced at service level

---

## Entity Relationships

```
┌──────────────────────────────────────────────────────────────────┐
│                                                                  │
│   ┌─────────┐     ┌─────────────┐     ┌─────────┐               │
│   │  User   │◄────│ GroupMember │────►│  Group  │               │
│   └────┬────┘     └─────────────┘     └────┬────┘               │
│        │                                    │                    │
│        │         ┌───────────────────┐      │                    │
│        │         │ PendingInvitation │      │                    │
│        │         └───────────────────┘      │                    │
│        │                  │                 │                    │
│        │     InvitedBy    │    GroupId      │                    │
│        └──────────────────┼─────────────────┘                    │
│                           │                                      │
│              LinkedUserId │ (after signup)                       │
│                           ▼                                      │
│                      ┌─────────┐                                 │
│                      │  User   │                                 │
│                      └─────────┘                                 │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Data Flow

### 1. Inviting a New Member

```
Admin adds email to group
         │
         ▼
   Email has account? ──Yes──► Create GroupMember
         │                            │
         No                           ▼
         │                     Send invitation email
         ▼                            │
  Create PendingInvitation            ▼
         │                       Done ✓
         ▼
  Send invitation email
         │
         ▼
     Done ✓
```

### 2. User Signs Up/In

```
User enters email + password
         │
         ▼
   Authenticate/Register
         │
         ▼
   Query PendingInvitation
   WHERE email = X
   AND Status = Pending
         │
         ▼
   For each invitation:
     ├── Create GroupMember (GroupId, UserId, Role=Member)
     ├── Update PendingInvitation.Status = Accepted
     └── Update PendingInvitation.LinkedUserId = userId
         │
         ▼
   User sees all groups on dashboard
```

---

## Migration Notes

- New `PendingInvitation` table will be created by SQLite on first access
- No migration of existing data required (new feature)
- Existing `GroupMember` records unaffected
