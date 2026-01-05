# Tasks: Cost-Sharing Application

**Input**: Design documents from `/specs/001-cost-sharing-app/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md  
**Architecture**: .NET MAUI cross-platform native application (Windows/macOS/Android/iOS)  
**Generated**: 2026-01-05

**Tests**: Tests are OPTIONAL and not included in this task list (not requested in feature specification)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md, the project structure is:
- Main app: `CostSharingApp/src/CostSharingApp/`
- Core library: `CostSharingApp/src/CostSharing.Core/`
- Tests: `CostSharingApp/tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and .NET MAUI setup

- [X] T001 Create solution structure: CostSharingApp.sln with CostSharingApp (MAUI), CostSharing.Core (library), and test projects
- [X] T002 Initialize .NET MAUI project in CostSharingApp/src/CostSharingApp/ with target frameworks for Android, iOS, Windows, MacCatalyst
- [X] T003 [P] Configure .editorconfig with C# and XAML formatting rules per constitution
- [X] T004 [P] Add NuGet packages: CommunityToolkit.Mvvm, Google.Apis.Drive.v3, SendGrid, Twilio, SQLite-net-pcl
- [X] T005 [P] Setup Roslyn analyzers and StyleCop in CostSharingApp.csproj for linting
- [X] T006 Create CostSharing.Core library project in CostSharingApp/src/CostSharing.Core/ for shared business logic
- [X] T007 [P] Configure dependency injection in CostSharingApp/MauiProgram.cs
- [X] T008 [P] Create platform-specific folders structure in CostSharingApp/Platforms/ (Android, iOS, Windows, MacCatalyst)
- [X] T009-DOCKER [P] Setup Docker build environment: Dockerfile, docker-compose.yml, .dockerignore, build script in CostSharingApp/build/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete
- [X] T010 Create base domain entities in CostSharing.Core/Models/: User.cs, Group.cs, GroupMember.cs, Invitation.cs, Expense.cs, ExpenseSplit.cs, Debt.cs, Settlement.cs
- [X] T011 [P] Implement Google Drive authentication service in CostSharingApp/Services/DriveAuthService.cs (OAuth flow for native apps)
- [X] T012 [P] Implement local SQLite cache service in CostSharingApp/Services/CacheService.cs for offline storage
- [X] T013 Implement DriveService base class in CostSharingApp/Services/DriveService.cs with file read/write/sync operations
- [X] T014 [P] Create base ViewModel class in CostSharingApp/ViewModels/BaseViewModel.cs with INotifyPropertyChanged
- [X] T015 [P] Setup app navigation shell in CostSharingApp/AppShell.xaml with route configuration
- [X] T016 [P] Implement error handling service in CostSharingApp/Services/ErrorService.cs for user-friendly error messages
- [X] T017 [P] Create logging service in CostSharingApp/Services/LoggingService.cs
- [X] T018 Implement authentication service in CostSharingApp/Services/AuthService.cs (email/password + magic link with local credential storage)
- [X] T019 [P] App resources and styles already exist in CostSharingApp/Resources/Styles/ (Colors.xaml, Styles.xaml - MAUI template default)
- [X] T020 [P] Setup environment configuration in CostSharingApp/appsettings.json (Google Drive API, SendGrid, Twilio credentials)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Group Creation and Basic Management (Priority: P1) 🎯 MVP

**Goal**: Enable users to create cost-sharing groups and manage basic group information

**Independent Test**: Create a group, view its details, update group name, verify data persists to Google Drive

### Implementation for User Story 1

- [X] T021 [P] [US1] Group and GroupMember models already complete in Phase 2 (T010)
- [X] T022 [P] [US1] Group and GroupMember models already complete in Phase 2 (T010)
- [X] T023 [US1] Implement GroupService in CostSharingApp/Services/GroupService.cs (CRUD operations, Google Drive sync)
- [X] T024 [US1] Group file storage implemented in DriveService (JSON serialization to Google Drive)
- [X] T025 [P] [US1] Create GroupListPage XAML in CostSharingApp/Views/Groups/GroupListPage.xaml (list of all user's groups)
- [X] T026 [P] [US1] Create GroupListViewModel in CostSharingApp/ViewModels/Groups/GroupListViewModel.cs with observable collection
- [X] T027 [P] [US1] Create CreateGroupPage XAML in CostSharingApp/Views/Groups/CreateGroupPage.xaml (form with group name input)
- [X] T028 [P] [US1] Create CreateGroupViewModel in CostSharingApp/ViewModels/Groups/CreateGroupViewModel.cs with validation
- [X] T029 [US1] Create GroupDetailsPage XAML in CostSharingApp/Views/Groups/GroupDetailsPage.xaml (show group info, members, expenses)
- [X] T030 [US1] Create GroupDetailsViewModel in CostSharingApp/ViewModels/Groups/GroupDetailsViewModel.cs
- [X] T031 [US1] Group update functionality implemented in GroupService.UpdateGroupAsync()
- [X] T032 [US1] Group deletion with confirmation dialog implemented in GroupDetailsViewModel.DeleteGroupAsync()
- [X] T033 [US1] Navigation routes registered in AppShell.xaml.cs (creategroup, groupdetails, editgroup)

**Checkpoint**: At this point, User Story 1 should be fully functional - users can create, view, update, and delete groups

---

## Phase 4: User Story 2 - Member Invitation and Management (Priority: P1) 🎯 MVP

**Goal**: Enable group creators to invite members via email/SMS and members to accept invitations

**Independent Test**: Create a group, send invitation via email or phone, accept invitation from recipient, verify member appears in group

### Implementation for User Story 2

- [X] T034 [P] [US2] Create Invitation model in CostSharing.Core/Models/Invitation.cs with token, status, expiration
- [X] T034 [P] [US2] Implement NotificationService in CostSharingApp/Services/NotificationService.cs (SendGrid SDK for email, Twilio SDK for SMS)
- [X] T035 [US2] Implement InvitationService in CostSharingApp/Services/InvitationService.cs (create invitations, generate tokens, validate)
- [X] T036 [P] [US2] Create InviteMemberPage XAML in CostSharingApp/Views/Members/InviteMemberPage.xaml (email/phone input, method selection)
- [X] T037 [P] [US2] Create InviteMemberViewModel in CostSharingApp/ViewModels/Members/InviteMemberViewModel.cs
- [X] T038 [US2] Implement invitation link generation with unique tokens (256-bit cryptographic random)
- [X] T039 [US2] Create invitation email template for SendGrid with variables: {inviterName}, {groupName}, {invitationLink}, {expirationDate}
- [X] T040 [US2] Create invitation SMS template for Twilio (max 256 chars with URL shortening)
- [X] T041 [P] [US2] Create AcceptInvitationPage XAML in CostSharingApp/Views/Members/AcceptInvitationPage.xaml (handles deep links)
- [X] T042 [P] [US2] Create AcceptInvitationViewModel in CostSharingApp/ViewModels/Members/AcceptInvitationViewModel.cs
- [X] T043 [US2] Implement deep link handling for invitation URLs in CostSharingApp/AppShell.xaml
- [X] T044 [US2] Add auto-join logic: if user not registered, prompt for account creation; if registered, add to group immediately
- [X] T045 [P] [US2] Create MemberListView user control in CostSharingApp/Views/Members/MemberListView.xaml (show members in GroupDetailsPage)
- [X] T046 [US2] Implement remove member functionality in GroupDetailsViewModel (admin only, prevent if member has expenses)
- [X] T047 [US2] Add pending invitations view in GroupDetailsPage with resend/cancel options
- [X] T048 [US2] Implement invitation expiration handling (7-day default, mark as expired, allow resend)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - groups with members can be created

---

## Phase 5: User Story 3 - Add and Split Expenses Evenly (Priority: P1) 🎯 MVP

**Goal**: Enable group members to add expenses and split them evenly among selected members

**Independent Test**: Add expense with description/amount, split evenly among selected members (including payer), verify calculated debts are correct

### Implementation for User Story 3

- [X] T049 [P] [US3] Create Expense model in CostSharing.Core/Models/Expense.cs with description, amount, payer, splitType
- [X] T050 [P] [US3] Create ExpenseSplit model in CostSharing.Core/Models/ExpenseSplit.cs with member, amount, percentage
- [X] T051 [P] [US3] Create Debt model in CostSharing.Core/Models/Debt.cs (calculated, not persisted)
- [X] T052 [US3] Implement ExpenseService in CostSharingApp/Services/ExpenseService.cs (CRUD operations, sync to Google Drive)
- [X] T053 [US3] Implement even split calculation in CostSharing.Core/Services/SplitCalculationService.cs (divide amount by participant count, handle rounding)
- [X] T054 [P] [US3] Create AddExpensePage XAML in CostSharingApp/Views/Expenses/AddExpensePage.xaml (description, amount, member selection)
- [X] T055 [P] [US3] Create AddExpenseViewModel in CostSharingApp/ViewModels/Expenses/AddExpenseViewModel.cs with validation (amount > 0, description 1-200 chars)
- [X] T056 [US3] Implement member multi-select UI in AddExpensePage (checkbox list or picker with "Select All" option)
- [X] T057 [US3] Add split type selection in AddExpensePage (Even/Custom radio buttons)
- [X] T058 [P] [US3] Create ExpenseListView user control in CostSharingApp/Views/Expenses/ExpenseListView.xaml (show in GroupDetailsPage)
- [X] T059 [P] [US3] Create ExpenseListViewModel in CostSharingApp/ViewModels/Expenses/ExpenseListViewModel.cs
- [X] T060 [US3] Create ExpenseDetailsPage XAML in CostSharingApp/Views/Expenses/ExpenseDetailsPage.xaml (show description, amount, payer, split breakdown)
- [X] T061 [US3] Create ExpenseDetailsViewModel in CostSharingApp/ViewModels/Expenses/ExpenseDetailsViewModel.cs
- [X] T062 [US3] Implement expense edit functionality (only creator can edit) in ExpenseDetailsViewModel
- [X] T063 [US3] Implement expense deletion with confirmation (only creator can delete) in ExpenseDetailsViewModel
- [X] T064 [US3] Implement debt calculation in CostSharing.Core/Services/DebtCalculationService.cs (calculate who owes whom from all expenses)
- [X] T065 [US3] Add debt summary view in GroupDetailsPage (show all debts: "Alice owes Bob $30")
- [X] T066 [US3] Implement recalculation of debts when expense is added/edited/deleted in ExpenseService
- [X] T067 [US3] Add navigation from GroupDetailsPage to AddExpensePage and ExpenseDetailsPage in AppShell.xaml

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work - complete MVP for even expense splitting

---

## Phase 6: User Story 4 - Custom Percentage Split (Priority: P2)

**Goal**: Enable custom percentage-based expense splitting for unequal shares

**Independent Test**: Add expense, assign custom percentages (e.g., 50%, 30%, 20%), verify percentages total 100%, verify amounts calculated correctly

### Implementation for User Story 4

- [X] T068 [P] [US4] Create CustomSplitPage XAML in CostSharingApp/Views/Expenses/CustomSplitPage.xaml (list members with percentage input fields)
- [X] T069 [P] [US4] Create CustomSplitViewModel in CostSharingApp/ViewModels/Expenses/CustomSplitViewModel.cs
- [X] T070 [US4] Implement custom split calculation in CostSharing.Core/Services/SplitCalculationService.cs (percentage validation, amount calculation)
- [X] T071 [US4] Add percentage validation: sum must equal 100%, display error if not
- [X] T072 [US4] Implement percentage-to-amount conversion with proper rounding (2 decimal places, extra penny to first member)
- [X] T073 [US4] Add 0% exclusion logic (member with 0% is not included in expense splits)
- [X] T074 [US4] Update ExpenseDetailsPage to show percentage breakdown for custom split expenses
- [X] T075 [US4] Add navigation from AddExpensePage to CustomSplitPage when "Custom" split type selected
- [X] T076 [US4] Implement real-time percentage sum display in CustomSplitPage (shows running total as user types)

**Checkpoint**: At this point, custom percentage splitting should work alongside even splitting

---

## Phase 7: User Story 5 - Debt Simplification (Priority: P2)

**Goal**: Minimize the number of transactions needed to settle all debts using Min-Cash-Flow algorithm

**Independent Test**: Create multiple expenses with different payers, view simplified settlement plan, verify transaction count is minimized

### Implementation for User Story 5

- [X] T077 [P] [US5] Create Settlement model in CostSharing.Core/Models/Settlement.cs (from/to user, amount, status)
- [X] T078 [US5] Implement Min-Cash-Flow algorithm in CostSharing.Core/Algorithms/DebtSimplificationAlgorithm.cs (calculate net balances, greedy matching)
- [X] T079 [US5] Implement net balance calculation in DebtSimplificationAlgorithm (total paid - total owed per member)
- [X] T080 [US5] Implement greedy matching: max creditor + max debtor, settle min amount, repeat
- [X] T081 [P] [US5] Create SimplifiedDebtsPage XAML in CostSharingApp/Views/Debts/SimplifiedDebtsPage.xaml (show settlement plan)
- [X] T082 [P] [US5] Create SimplifiedDebtsViewModel in CostSharingApp/ViewModels/Debts/SimplifiedDebtsViewModel.cs
- [X] T083 [US5] Add toggle in GroupDetailsPage debt summary: "Show Detailed" vs "Show Simplified"
- [X] T084 [US5] Implement settlement recording: mark debt as paid in SimplifiedDebtsPage
- [X] T085 [US5] Create SettlementService in CostSharingApp/Services/SettlementService.cs (persist settlements to Google Drive)
- [X] T086 [US5] Add settlement history view in GroupDetailsPage (show past payments)
- [X] T087 [US5] Update debt calculations to account for recorded settlements (reduce outstanding balances)
- [X] T088 [US5] Add automatic recalculation of simplified debts when new expense added or settlement recorded

**Checkpoint**: At this point, debt simplification should work, reducing transaction complexity

---

## Phase 8: User Story 6 - Personal Balance and History (Priority: P3)

**Goal**: Provide users with comprehensive view of balances across all groups and transaction history

**Independent Test**: View dashboard showing total balance across all groups, filter transaction history by date, verify accuracy

### Implementation for User Story 6

- [ ] T089 [P] [US6] Create DashboardPage XAML in CostSharingApp/Views/DashboardPage.xaml (show total balance, per-group balances)
- [ ] T090 [P] [US6] Create DashboardViewModel in CostSharingApp/ViewModels/DashboardViewModel.cs
- [ ] T091 [US6] Implement total balance calculation in DashboardViewModel (sum across all groups: owed - owing)
- [ ] T092 [US6] Add per-group balance display in DashboardPage (show each group's balance with color coding: green for owed, red for owing)
- [ ] T093 [P] [US6] Create TransactionHistoryPage XAML in CostSharingApp/Views/TransactionHistoryPage.xaml (list all user's expenses)
- [ ] T094 [P] [US6] Create TransactionHistoryViewModel in CostSharingApp/ViewModels/TransactionHistoryViewModel.cs
- [ ] T095 [US6] Implement date range filter in TransactionHistoryPage (start date, end date pickers)
- [ ] T096 [US6] Implement transaction type filter (paid by me, owe for, all)
- [ ] T097 [US6] Add visual indicators for transaction types: badge for "You paid", different color for "You owe"
- [ ] T098 [US6] Implement navigation from DashboardPage to GroupDetailsPage (tap on group balance)
- [ ] T099 [US6] Implement navigation from DashboardPage to TransactionHistoryPage
- [ ] T100 [US6] Set DashboardPage as app startup page in AppShell.xaml

**Checkpoint**: At this point, users have complete visibility into their financial position across all groups

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T101 [P] Add offline mode indicators in app UI (show sync status in app bar)
- [ ] T102 [P] Implement background sync service in CostSharingApp/Services/BackgroundSyncService.cs (sync local changes to Google Drive when online)
- [ ] T103 [P] Add conflict resolution UI for Google Drive sync conflicts (show diff, let user choose version)
- [ ] T104 [P] Implement currency symbol display ($) for all monetary amounts app-wide
- [ ] T105 [P] Add input validation error messages with user-friendly text across all forms
- [ ] T106 [P] Implement loading spinners for async operations (group load, expense add, sync)
- [ ] T107 [P] Add empty state messages for all list views ("No groups yet", "No expenses", "No debts")
- [ ] T108 [P] Implement pull-to-refresh on all list pages (GroupListPage, ExpenseListView, etc.)
- [ ] T109 [P] Create app icon and splash screen in CostSharingApp/Resources/AppIcon/
- [ ] T110 [P] Setup Android app signing in CostSharingApp/Platforms/Android/AndroidManifest.xml
- [ ] T111 [P] Setup iOS provisioning profile configuration
- [ ] T112 [P] Create Windows MSIX packaging configuration in CostSharingApp/Platforms/Windows/
- [ ] T113 [P] Add XML documentation comments to all public APIs in CostSharing.Core
- [ ] T114 [P] Update README.md with build instructions for each platform
- [ ] T115 [P] Create GitHub Actions workflow in .github/workflows/build.yml for multi-platform builds
- [ ] T116 Run quickstart.md validation (verify developer can set up environment)
- [ ] T117 Code cleanup and refactoring across all files
- [ ] T118 Performance optimization: implement caching for frequently accessed data
- [ ] T119 Security review: ensure Google Drive credentials stored securely, no secrets in code

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - US1, US2, US3 (P1 - MVP): Should complete first (can be parallel if team capacity)
  - US4, US5 (P2): Can start after Foundational (can be parallel)
  - US6 (P3): Can start after Foundational
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational - Integrates with US1 (adds members to groups)
- **User Story 3 (P1)**: Can start after Foundational - Integrates with US1 & US2 (expenses in groups with members)
- **User Story 4 (P2)**: Can start after Foundational - Extends US3 (adds custom split option)
- **User Story 5 (P2)**: Can start after Foundational - Uses US3 data (calculates simplified debts from expenses)
- **User Story 6 (P3)**: Can start after Foundational - Uses US1, US3, US5 data (dashboard shows all balances)

**Practical Execution**: Complete in sequence for single developer: Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 → Phase 7 → Phase 8 → Phase 9

### Within Each User Story

- Models before services (e.g., T020-T021 before T022)
- Services before ViewModels (e.g., T022 before T025)
- Views and ViewModels in parallel (marked [P])
- Core implementation before navigation/integration (e.g., T054-T057 before T067)

### Parallel Opportunities

**Within Setup (Phase 1)**:
- T003, T004, T005 (config files) can run in parallel
- T007, T008 (DI and platform folders) can run in parallel

**Within Foundational (Phase 2)**:
- T010, T011, T013, T014, T015, T016, T018, T019 (independent services and config) can run in parallel after T009

**Within Each User Story**:
- All View XAML files can be created in parallel
- All ViewModel classes can be created in parallel (after services exist)
- Example US3: T054-T055, T058-T059, T060-T061 can all run in parallel

**Across User Stories** (if team capacity):
- After Phase 2 completes, US1, US2, US3 can be worked on by different developers simultaneously
- US4 and US5 can be worked on in parallel
- All Phase 9 polish tasks marked [P] can run in parallel

---

## Parallel Example: User Story 3 (Even Split)

```bash
# After T051 completes, launch all View/ViewModel pairs together:
Task T054: "Create AddExpensePage XAML in CostSharingApp/Views/Expenses/AddExpensePage.xaml"
Task T055: "Create AddExpenseViewModel in CostSharingApp/ViewModels/Expenses/AddExpenseViewModel.cs"
Task T058: "Create ExpenseListView user control in CostSharingApp/Views/Expenses/ExpenseListView.xaml"
Task T059: "Create ExpenseListViewModel in CostSharingApp/ViewModels/Expenses/ExpenseListViewModel.cs"
Task T060: "Create ExpenseDetailsPage XAML in CostSharingApp/Views/Expenses/ExpenseDetailsPage.xaml"
Task T061: "Create ExpenseDetailsViewModel in CostSharingApp/ViewModels/Expenses/ExpenseDetailsViewModel.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1, 2, 3 Only)

1. **Complete Phase 1: Setup** (T001-T008) → Project initialized
2. **Complete Phase 2: Foundational** (T009-T019) → Foundation ready (CRITICAL - blocks all stories)
3. **Complete Phase 3: User Story 1** (T020-T032) → Groups work
4. **Complete Phase 4: User Story 2** (T033-T048) → Invitations work
5. **Complete Phase 5: User Story 3** (T049-T067) → Even expense splitting works
6. **STOP and VALIDATE**: Test all three stories independently
7. **Deploy MVP**: Build .exe/.apk/.dmg installers and distribute

**MVP = 67 tasks** (T001-T067)

### Incremental Delivery

1. Setup + Foundational (T001-T019) → Foundation ready
2. Add US1 (T020-T032) → Test independently → Users can manage groups
3. Add US2 (T033-T048) → Test independently → Users can invite members  
4. Add US3 (T049-T067) → Test independently → **MVP COMPLETE** - Even splitting works
5. Add US4 (T068-T076) → Test independently → Custom splitting available
6. Add US5 (T077-T088) → Test independently → Debt simplification available
7. Add US6 (T089-T100) → Test independently → Dashboard and history available
8. Polish (T101-T119) → Production-ready application

### Parallel Team Strategy

With 3 developers:

1. **All together**: Complete Setup (Phase 1) + Foundational (Phase 2) → T001-T019
2. **Once Foundational is done**:
   - **Developer A**: User Story 1 (T020-T032) - Groups
   - **Developer B**: User Story 2 (T033-T048) - Invitations (needs US1 for integration testing)
   - **Developer C**: Start on shared services or wait
3. **Next iteration**:
   - **Developer A**: User Story 4 (T068-T076) - Custom split
   - **Developer B**: User Story 3 (T049-T067) - Even split (depends on US1 & US2)
   - **Developer C**: User Story 5 (T077-T088) - Debt simplification
4. **Final iteration**:
   - **Any developer**: User Story 6 (T089-T100) - Dashboard
   - **All together**: Polish (T101-T119)

---

## Task Count Summary

- **Phase 1 (Setup)**: 8 tasks
- **Phase 2 (Foundational)**: 11 tasks (BLOCKS all user stories)
- **Phase 3 (US1 - Groups)**: 13 tasks
- **Phase 4 (US2 - Invitations)**: 16 tasks
- **Phase 5 (US3 - Even Split)**: 19 tasks
- **Phase 6 (US4 - Custom Split)**: 9 tasks
- **Phase 7 (US5 - Debt Simplification)**: 12 tasks
- **Phase 8 (US6 - Dashboard)**: 12 tasks
- **Phase 9 (Polish)**: 19 tasks

**Total: 119 tasks**

**MVP Scope (P1 - US1, US2, US3)**: 67 tasks (Setup + Foundational + US1 + US2 + US3)

**Post-MVP (P2 - US4, US5)**: 21 tasks

**Enhanced (P3 - US6)**: 12 tasks

**Production Ready (Polish)**: 19 tasks

---

## Parallel Opportunities Summary

- **18 tasks** in Setup/Foundational can run in parallel (marked [P])
- **Within each user story**: 30+ View/ViewModel pairs can be created in parallel
- **Across user stories**: After Foundational, up to 6 stories can be worked on simultaneously (with team capacity)
- **Polish phase**: 15+ tasks can run in parallel (documentation, config, optimization)

**Estimated total parallel opportunities**: 60+ tasks (50% of total)

---

## Notes

- **[P] tasks**: Different files, no dependencies - safe to parallelize
- **[Story] labels**: Map task to specific user story for traceability
- **File paths**: All paths are exact and based on plan.md structure
- **MVP focus**: Complete T001-T067 for minimum viable product (even expense splitting)
- **Constitutional compliance**: All tasks follow component-based architecture, linting enforced from T005
- **Testing strategy**: No tests included (not requested in spec) - focus on implementation
- **Commit strategy**: Commit after each task or logical group (e.g., commit View + ViewModel together)
- **Validation checkpoints**: Stop at end of each user story phase to validate independently

---

## Independent Test Criteria

Each user story should be independently testable:

- **US1**: Create group → View in list → Update name → Delete → Verify Google Drive file operations
- **US2**: Invite via email → Invite via SMS → Accept invitation → Verify member in group → Remove member
- **US3**: Add expense → Split evenly → View debts → Edit expense → Delete expense → Verify recalculation
- **US4**: Add expense with custom % → Verify sum=100% validation → Verify amounts correct → View breakdown
- **US5**: Create multiple expenses → View simplified debts → Verify transaction count reduced → Record settlement
- **US6**: View dashboard balance → Filter history by date → Verify calculations across all groups

---

## Suggested MVP Scope

**Recommended MVP**: User Stories 1, 2, and 3 (P1 priority)

This delivers:
- ✅ Group creation and management
- ✅ Member invitations (email/SMS)
- ✅ Even expense splitting
- ✅ Basic debt tracking
- ✅ Core value proposition: "Track who owes what in shared expenses"

**Estimated effort**: 67 tasks (56% of total project)

**What's missing in MVP**:
- ❌ Custom percentage splits (US4 - P2)
- ❌ Debt simplification algorithm (US5 - P2)
- ❌ Dashboard and history (US6 - P3)
- ❌ Polish and production features (Phase 9)

**Recommended post-MVP additions**:
1. **Next**: Add US5 (Debt Simplification) - high value, key differentiator
2. **Then**: Add US4 (Custom Split) - handles complex scenarios
3. **Finally**: Add US6 (Dashboard) - nice-to-have visibility
4. **Production**: Complete Phase 9 - installer packaging, optimization, security

---