# Tasks: Gmail Invitation & Member Sync

**Input**: Design documents from `/specs/004-gmail-invite-sync/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Not explicitly requested in specification - omitting test tasks.

**Organization**: Tasks grouped by user story (P1, P1, P1, P2) to enable independent implementation.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- File paths relative to `CostSharingApp/src/`

---

## Phase 1: Setup

**Purpose**: Create new model and interface files

- [X] T001 [P] Create InvitationStatus enum in CostSharing.Core/Models/InvitationStatus.cs (added Cancelled=4 to existing enum in Invitation.cs)
- [X] T002 [P] Create InvitationType enum in CostSharing.Core/Models/InvitationType.cs
- [X] T003 [P] Create InvitationResult record in CostSharing.Core/Models/InvitationResult.cs
- [X] T004 Create PendingInvitation model in CostSharing.Core/Models/PendingInvitation.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core service interface and implementation that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 Create IInvitationLinkingService interface in CostSharing.Core/Interfaces/IInvitationLinkingService.cs
- [X] T006 Create InvitationLinkingService class in CostSharingApp/Services/InvitationLinkingService.cs with constructor and dependencies
- [X] T007 Register IInvitationLinkingService in CostSharingApp/MauiProgram.cs

**Checkpoint**: Foundation ready - user story implementation can now begin ✅

---

## Phase 3: User Story 1 - Send Gmail Invitation When Adding Member (Priority: P1) 🎯 MVP

**Goal**: When admin adds member by email, send invitation via Gmail

**Independent Test**: Add member by email in group → verify invitation email received

### Implementation for User Story 1

- [X] T008 [US1] Implement InviteToGroupAsync method in CostSharingApp/Services/InvitationLinkingService.cs
- [X] T009 [US1] Implement IsAlreadyMemberOrPendingAsync method in CostSharingApp/Services/InvitationLinkingService.cs
- [X] T010 [US1] Add email validation helper method (case-insensitive normalization) in InvitationLinkingService
- [X] T011 [US1] Add admin permission check in InviteToGroupAsync (verify inviter is GroupRole.Admin)
- [X] T012 [US1] Integrate Gmail sending in InviteToGroupAsync (call IGmailInvitationService.SendInvitationAsync)
- [X] T013 [US1] Handle Gmail authorization check (call IsGmailAuthorizedAsync before sending)
- [X] T014 [US1] Implement non-blocking email failure handling (member added even if email fails)
- [X] T015 [P] [US1] Refactored InviteMemberViewModel to use IInvitationLinkingService (email-only invites)
- [X] T016 [P] [US1] Updated InviteMemberPage.xaml for email-only input
- [X] T017 [US1] Bind InviteMemberCommand to UI and display success/error toast messages

**Checkpoint**: User Story 1 complete - admins can invite members by email with Gmail notification ✅

---

## Phase 4: User Story 2 - See My Groups After Sign In (Priority: P1)

**Goal**: Users see groups they were invited to when signing in

**Independent Test**: Invite email to group → sign up with that email → verify group appears on dashboard

### Implementation for User Story 2

- [X] T018 [US2] Implement LinkPendingInvitationsAsync method in CostSharingApp/Services/InvitationLinkingService.cs
- [X] T019 [US2] Query PendingInvitation by normalized email where Status = Pending
- [X] T020 [US2] Create GroupMember for each matching invitation (GroupId, UserId, Role=Member)
- [X] T021 [US2] Update PendingInvitation.Status to Accepted and set LinkedUserId
- [X] T022 [US2] Modify AuthService.RegisterAsync to call LinkPendingInvitationsAsync after user creation in CostSharingApp/Services/AuthService.cs
- [X] T023 [US2] Modify AuthService.LoginAsync to call LinkPendingInvitationsAsync after authentication in CostSharingApp/Services/AuthService.cs

**Checkpoint**: User Story 2 complete - users see their groups immediately after sign in ✅

---

## Phase 5: User Story 3 - Prevent Duplicate Members in a Group (Priority: P1)

**Goal**: Reject attempts to add duplicate members with clear error message

**Independent Test**: Add same email twice to group → verify second attempt shows error

### Implementation for User Story 3

- [X] T024 [US3] Enhance IsAlreadyMemberOrPendingAsync to check both GroupMember and PendingInvitation tables
- [X] T025 [US3] Return appropriate error message in InviteToGroupAsync when duplicate detected
- [X] T026 [US3] Display user-friendly error "[email] is already a member of this group" in UI

**Checkpoint**: User Story 3 complete - duplicate members prevented with clear feedback ✅

---

## Phase 6: User Story 4 - Link Existing User to Group on Sign In (Priority: P2)

**Goal**: Existing users see new groups they were added to while offline

**Independent Test**: Existing user gets invited while logged out → log in → verify new group appears

### Implementation for User Story 4

- [X] T027 [US4] Implement GetPendingInvitationsAsync method in InvitationLinkingService
- [X] T028 [US4] Implement CancelInvitationAsync method in InvitationLinkingService (admin can revoke pending invitation)
- [X] T029 [P] [US4] Add pending invitations display section to GroupDetailsPage.xaml (show who is pending)
- [X] T030 [P] [US4] Add cancel invitation button for each pending member in UI

**Checkpoint**: User Story 4 complete - existing users auto-linked, admins can manage pending invitations ✅

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, cleanup, and edge cases

- [X] T031 [P] Add XML documentation comments to IInvitationLinkingService interface
- [X] T032 [P] Add XML documentation comments to InvitationLinkingService implementation
- [X] T033 [P] Add XML documentation comments to PendingInvitation model
- [X] T034 Add logging for invitation operations (success, failure, linking) in InvitationLinkingService
- [X] T035 Validate build completes successfully
- [X] T036 Run build validation - fixed duplicate InvitationStatus enum and ambiguous Group reference

---

## Phase 8: User Story 5 - Revoke Drive Access on Member Removal (Priority: P1)

**Goal**: When member is removed from group, revoke their Drive folder access

**Independent Test**: Remove member from group → verify they can no longer access Drive folder

### Implementation for User Story 5

- [X] T037 [US5] Add RemoveFolderPermissionAsync to IDriveSyncService interface in CostSharing.Core/Interfaces/IDriveSyncService.cs
- [X] T038 [US5] Implement RemoveFolderPermissionAsync in DriveSyncService.cs (list permissions, find by email, delete)
- [X] T039 [US5] Add IServiceProvider to GroupService constructor for lazy DriveSyncService resolution
- [X] T040 [US5] Create UnshareFolderWithMemberAsync helper method in GroupService.cs
- [X] T041 [US5] Modify RemoveMemberAsync to call UnshareFolderWithMemberAsync after deleting GroupMember

**Checkpoint**: User Story 5 complete - removed members lose Drive folder access ✅

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion - BLOCKS all user stories
- **User Stories (Phases 3-6)**: All depend on Phase 2 completion
  - US1 (Phase 3): Can start first - core MVP
  - US2 (Phase 4): Can start after Phase 2, but logically follows US1
  - US3 (Phase 5): Refines US1 duplicate checking
  - US4 (Phase 6): Independent, can run parallel to US3 if needed
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

| Story | Depends On | Can Parallel With |
|-------|------------|-------------------|
| US1 (Send Invite) | Phase 2 only | - |
| US2 (See Groups) | Phase 2 only | US1 (but US1 creates invitations to link) |
| US3 (Prevent Dupe) | US1 (enhances it) | - |
| US4 (Link Existing) | Phase 2 only | US3 |
| US5 (Revoke Drive) | Phase 2 only | US3, US4 |

### Within Each User Story

1. Service methods first (business logic)
2. ViewModel commands second (binding)
3. UI changes last (presentation)

### Parallel Opportunities per Phase

**Phase 1 (Setup)**:
```
T001, T002, T003 can run in parallel (different files)
T004 can run after T001 (depends on InvitationStatus enum)
```

**Phase 3 (US1)**:
```
T015, T016 can run in parallel (ViewModel and View are different files)
```

**Phase 6 (US4)**:
```
T029, T030 can run in parallel (both UI additions)
```

**Phase 7 (Polish)**:
```
T031, T032, T033 can run in parallel (different files)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T007)
3. Complete Phase 3: User Story 1 (T008-T017)
4. **STOP and VALIDATE**: Test invitation sending manually
5. Deploy if ready - admins can now invite members

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → Test → Deploy (MVP - invitations work!)
3. Add User Story 2 → Test → Deploy (invited users see groups)
4. Add User Story 3 → Test → Deploy (duplicates prevented)
5. Add User Story 4 → Test → Deploy (existing user linking + management UI)
6. Polish phase → Final validation

---

## Notes

- Email normalization: Use `email.Trim().ToLowerInvariant()` everywhere
- Gmail errors are non-blocking: Member/invitation created even if email fails
- Only admins (GroupRole.Admin) can invite members
- PendingInvitation table created automatically by SQLite on first access
- Commit after each task or logical group for clean history
