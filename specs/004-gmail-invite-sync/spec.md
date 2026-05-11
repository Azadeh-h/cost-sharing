# Feature Specification: Gmail Invitation & Member Sync

**Feature Branch**: `004-gmail-invite-sync`  
**Created**: 2026-01-22  
**Updated**: 2026-01-23  
**Status**: Implemented  
**Input**: User description: "Gmail invitation for group members - when adding a member use Gmail to send invite, when they sign in with same email they see their groups"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Send Gmail Invitation When Adding Member (Priority: P1)

As a group admin, when I add a new member by email, I want the system to send them an invitation email via Gmail so they know they've been invited to the group.

**Why this priority**: Core invitation mechanism - without email notification, invitees wouldn't know they've been added to a group.

**Independent Test**: Can be tested by adding a member via email in a group and verifying an invitation email is received in their inbox.

**Acceptance Scenarios**:

1. **Given** I am a group admin, **When** I add a member by entering their email address, **Then** an invitation email is sent to that address via Gmail.

2. **Given** I have entered a valid email for a new member, **When** the invitation is sent successfully, **Then** I see a confirmation message "Invitation sent to [email]".

3. **Given** Gmail authorization has not been granted, **When** I try to add a member, **Then** I am prompted to authorize Gmail access first.

4. **Given** the email address is invalid or Gmail fails, **When** sending the invitation, **Then** I see an error message and the member is still added to the group (invitation email is optional, not blocking).

---

### User Story 2 - See My Groups After Sign In (Priority: P1)

As a user who was invited to groups, when I sign in with my email, I want to automatically see all the groups I'm a member of so I can immediately participate in expense sharing.

**Why this priority**: Essential for the invitation flow to be complete - without this, invited users would have an empty dashboard even though they're members of groups.

**Independent Test**: Can be tested by inviting an email to a group, having that person sign up with the same email, and verifying they see the group in their dashboard.

**Acceptance Scenarios**:

1. **Given** my email was added as a member to one or more groups, **When** I sign in with that email address, **Then** I see all those groups in my dashboard.

2. **Given** I was invited to a group before I had an account, **When** I create a new account with the invited email, **Then** I am automatically linked to my existing group memberships.

3. **Given** I am a member of Group A and Group B, **When** I view my groups list, **Then** I see both groups with their names and member counts.

---

### User Story 3 - Prevent Duplicate Members in a Group (Priority: P1)

As a group admin, when I try to add someone who is already a member, I want to see an error so I don't accidentally add duplicates.

**Why this priority**: Data integrity is critical - duplicate members would corrupt expense calculations.

**Independent Test**: Can be tested by trying to add the same email to a group twice and verifying the second attempt is rejected.

**Acceptance Scenarios**:

1. **Given** a user with email X is already a member of Group A, **When** I try to add email X to Group A again, **Then** I see an error "[email] is already a member of this group".

2. **Given** a user is a member of Group A, **When** I add them to Group B, **Then** the addition succeeds (same user can be in multiple groups).

---

### User Story 4 - Link Existing User to Group on Sign In (Priority: P2)

As an existing user who was recently invited to new groups, when I sign in, I want to see any groups I've been added to since my last login.

**Why this priority**: Important for active users who are invited to new groups, but handled as part of normal sync flow.

**Independent Test**: Can be tested by having an existing user get invited to a new group while logged out, then logging in and verifying the new group appears.

**Acceptance Scenarios**:

1. **Given** I have an existing account with email X, **When** another user adds my email to their group, **Then** my account is linked to that group membership.

2. **Given** I was offline and someone added me to a new group, **When** I open the app and sign in, **Then** the new group appears in my list.

---

### User Story 5 - Revoke Drive Access When Member Removed (Priority: P1)

As a group admin, when I remove a member from the group, I want their access to the shared Google Drive folder to be automatically revoked so they can no longer see or modify the group's data.

**Why this priority**: Critical for data security and privacy - removed members should not retain access to group data.

**Independent Test**: Can be tested by removing a member from a group and verifying they can no longer access the shared Drive folder.

**Acceptance Scenarios**:

1. **Given** I am a group admin, **When** I remove a member from the group, **Then** their access to the group's Google Drive folder is automatically revoked.

2. **Given** the removed member had "writer" access to the folder, **When** they are removed, **Then** they can no longer view, edit, or access the folder contents.

3. **Given** Google Drive API is unavailable, **When** I remove a member, **Then** the member is still removed from the group (folder unshare is best-effort, logged for retry).

4. **Given** a member has a device-generated email (@device.local), **When** they are removed, **Then** no Drive unshare attempt is made (skip silently).

---

### Edge Cases

- **Case-insensitive email matching**: John@Example.com and john@example.com should be treated as the same user.
- **Gmail rate limits**: If Gmail rate limits are hit, show error to user but still add member to group (invitation email is best-effort).
- **Re-invitation after removal**: If a member removes themselves and is re-invited, they should be re-added as a new member (not rejected as duplicate).
- **Non-admin invitation attempt**: Only admins can add members; show permission error for non-admins.
- **Pending invitations sync**: The pending member record should persist and link when they sign up, even if group syncs via Google Drive before invited user creates account.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST send an invitation email via Gmail when a member is added to a group
- **FR-002**: System MUST match invited email addresses to signed-in users (case-insensitive)
- **FR-003**: System MUST prevent duplicate members within the same group (GroupId + Email must be unique)
- **FR-004**: System MUST allow the same user to be a member of multiple groups
- **FR-005**: System MUST prompt for Gmail authorization if not already granted when sending invitations
- **FR-006**: System MUST show user's groups on dashboard after sign in by matching their email to group memberships
- **FR-007**: System MUST link existing user accounts to group memberships when the email matches
- **FR-008**: System MUST create pending membership records for emails that don't yet have accounts
- **FR-009**: System MUST display clear error messages when adding duplicate members
- **FR-010**: System MUST continue with member addition even if invitation email fails (non-blocking)
- **FR-011**: System MUST verify that only group admins can add members
- **FR-012**: System MUST revoke Google Drive folder access when a member is removed from a group
- **FR-013**: System MUST continue with member removal even if Drive permission removal fails (non-blocking)

### Key Entities

- **User**: Account with email as unique identifier. Can be a member of many groups.
- **Group**: Contains multiple members. Cannot have the same user twice.
- **GroupMember**: Junction table linking User to Group. Unique constraint on (GroupId, UserId). Tracks role (Admin/Member), join date, and who added them.
- **PendingInvitation**: Tracks invitations sent to emails that don't have accounts yet. Links to User record once they sign up.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of member additions trigger an invitation email (when Gmail is authorized)
- **SC-002**: 100% of users signing in with an invited email see their groups immediately
- **SC-003**: 0% duplicate members can exist within a single group
- **SC-004**: Users can be members of unlimited groups (no artificial limit)
- **SC-005**: Email matching is 100% case-insensitive
- **SC-006**: Invitation email failures do not block member addition (graceful degradation)
- **SC-007**: 100% of member removals trigger Drive permission revocation attempt (when Drive folder exists)
- **SC-008**: Drive permission removal failures do not block member removal (graceful degradation)
