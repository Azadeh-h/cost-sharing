# Tasks: Email-Based Authentication

**Input**: Design documents from `/specs/003-email-auth/`
**Prerequisites**: plan.md (required), spec.md (required for user stories)

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Core library**: `CostSharingApp/src/CostSharing.Core/`
- **MAUI app**: `CostSharingApp/src/CostSharingApp/`
- **Tests**: `CostSharingApp/tests/CostSharingApp.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create core services and model updates needed for authentication

- [x] T001 Add AccountType enum (Device, Email) to CostSharingApp/src/CostSharing.Core/Models/User.cs
- [x] T002 Add AccountType property to User model in CostSharingApp/src/CostSharing.Core/Models/User.cs
- [x] T003 [P] Create ISessionService interface in CostSharingApp/src/CostSharingApp/Services/ISessionService.cs
- [x] T004 Create SessionService for session persistence in CostSharingApp/src/CostSharingApp/Services/SessionService.cs
- [x] T005 [P] Create Auth folder structure for Views in CostSharingApp/src/CostSharingApp/Views/Auth/
- [x] T006 [P] Create Auth folder structure for ViewModels in CostSharingApp/src/CostSharingApp/ViewModels/Auth/
- [x] T007 Register ISessionService in MauiProgram.cs in CostSharingApp/src/CostSharingApp/MauiProgram.cs

**Checkpoint**: Infrastructure ready - authentication UI can be built

---

## Phase 2: User Story 1 - Sign Up with Email (Priority: P1) 🎯 MVP

**Goal**: New users can create an account with email, password, and display name

**Independent Test**: Launch app → See auth page → Fill sign up form → Account created → Navigate to dashboard

### Implementation for User Story 1

- [x] T008 [US1] Create AuthPage.xaml with Sign Up form UI in CostSharingApp/src/CostSharingApp/Views/Auth/AuthPage.xaml
- [x] T009 [US1] Create AuthPage.xaml.cs code-behind in CostSharingApp/src/CostSharingApp/Views/Auth/AuthPage.xaml.cs
- [x] T010 [US1] Create AuthViewModel with sign up state management in CostSharingApp/src/CostSharingApp/ViewModels/Auth/AuthViewModel.cs
- [x] T011 [US1] Add email validation method to AuthViewModel (valid email format check)
- [x] T012 [US1] Add password validation method to AuthViewModel (8+ chars, 1+ number)
- [x] T013 [US1] Add password confirmation validation to AuthViewModel
- [x] T014 [US1] Add SignUpCommand to AuthViewModel that calls AuthService.RegisterAsync
- [x] T015 [US1] Add error display for duplicate email in AuthPage.xaml
- [ ] T016 [US1] Update AuthService.RegisterAsync to set AccountType.Email for new email accounts

**Checkpoint**: User Story 1 complete - users can sign up with email

---

## Phase 3: User Story 2 - Sign In with Email (Priority: P1)

**Goal**: Returning users can sign in with email and password

**Independent Test**: Sign up → Sign out → Sign in with same credentials → Navigate to dashboard

### Implementation for User Story 2

- [x] T017 [US2] Add Sign In form UI to AuthPage.xaml (toggle between sign up/sign in)
- [x] T018 [US2] Add IsSignInMode property to AuthViewModel for toggling views
- [x] T019 [US2] Add ToggleModeCommand to AuthViewModel to switch between sign in/sign up
- [x] T020 [US2] Add SignInCommand to AuthViewModel that calls AuthService.LoginAsync
- [x] T021 [US2] Add error display for invalid credentials in AuthPage.xaml
- [x] T022 [US2] Add error display for account not found in AuthPage.xaml

**Checkpoint**: User Story 2 complete - users can sign in with email

---

## Phase 4: User Story 3 - Stay Signed In (Priority: P1)

**Goal**: Users remain signed in after closing the app until explicit sign out

**Independent Test**: Sign in → Close app → Reopen app → Still signed in → Dashboard shown

### Implementation for User Story 3

- [x] T023 [US3] Implement SaveSessionAsync in SessionService to store user ID in SecureStorage
- [x] T024 [US3] Implement GetSessionAsync in SessionService to retrieve saved session
- [x] T025 [US3] Implement ClearSessionAsync in SessionService to remove session on sign out
- [x] T026 [US3] Update AuthService.LoginAsync to call SessionService.SaveSessionAsync after successful login
- [x] T027 [US3] Update AuthService.RegisterAsync to call SessionService.SaveSessionAsync after successful registration
- [x] T028 [US3] Modify App.xaml.cs to check session on startup and restore user
- [x] T029 [US3] Add InitializeSessionAsync method to AuthService to restore user from session
- [x] T030 [US3] Navigate to Dashboard if session valid, AuthPage if not in App.xaml.cs
- [x] T031 [US3] Update SettingsViewModel LogOutAsync to call SessionService.ClearSessionAsync
- [x] T032 [US3] Navigate to AuthPage after sign out in SettingsViewModel

**Checkpoint**: User Story 3 complete - session persistence works

---

## Phase 5: App Navigation Integration

**Purpose**: Wire up authentication flow with app navigation

- [x] T033 Register AuthPage route in AppShell.xaml.cs
- [x] T034 Add AuthPage to AppShell.xaml (hidden from flyout menu)
- [ ] T035 Remove auto-login device account creation from GroupService.cs
- [ ] T036 Update any code that creates device.local accounts to require authentication first

**Checkpoint**: Navigation complete - auth flow integrated with app

---

## Phase 6: User Story 4 - Migrate Device Account to Email (Priority: P2)

**Goal**: Existing device account users can add email and password to their account

**Independent Test**: Have device account with groups → Add email → Sign out → Sign in with email → Data preserved

### Implementation for User Story 4

- [ ] T037 [US4] Add IsDeviceAccount property to User model (checks if email ends with @device.local)
- [ ] T038 [US4] Add "Add Email to Account" button to EditProfilePage.xaml (visible only for device accounts)
- [ ] T039 [US4] Create MigrateAccountCommand in EditProfileViewModel
- [ ] T040 [US4] Add migration form UI to EditProfilePage (email, password, confirm password)
- [ ] T041 [US4] Add MigrateToEmailAsync method to AuthService
- [ ] T042 [US4] Update user email, password hash, and AccountType in MigrateToEmailAsync
- [ ] T043 [US4] Update session with new credentials after migration

**Checkpoint**: User Story 4 complete - device accounts can migrate to email

---

## Phase 7: Polish & Testing

**Purpose**: Error handling, validation, and testing

- [ ] T044 [P] Add inline validation error display for all form fields
- [ ] T045 [P] Add loading indicator during sign in/sign up operations
- [ ] T046 Add keyboard handling (next field, submit on enter)
- [ ] T047 [P] Create unit tests for email validation in CostSharingApp/tests/CostSharingApp.Tests/ViewModels/AuthViewModelTests.cs
- [ ] T048 [P] Create unit tests for password validation in CostSharingApp/tests/CostSharingApp.Tests/ViewModels/AuthViewModelTests.cs
- [ ] T049 [P] Create unit tests for SessionService in CostSharingApp/tests/CostSharingApp.Tests/Services/SessionServiceTests.cs
- [ ] T050 Manual testing: Verify sign up flow on Android
- [ ] T051 Manual testing: Verify sign in flow on Android
- [ ] T052 Manual testing: Verify session persistence on Android

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **User Story 1 (Phase 2)**: Depends on Phase 1 completion
- **User Story 2 (Phase 3)**: Depends on Phase 2 (builds on AuthPage)
- **User Story 3 (Phase 4)**: Depends on Phase 2 and Phase 3
- **Navigation (Phase 5)**: Depends on Phase 4
- **User Story 4 (Phase 6)**: Depends on Phase 5 - can be deferred for MVP
- **Polish (Phase 7)**: Depends on all user stories being complete

### Parallel Opportunities

**Within Phase 1:**
```
T003, T005, T006 - all can run in parallel (different files)
```

**Within Phase 7:**
```
T044, T045, T047, T048, T049 - all independent
```

---

## Implementation Strategy

### MVP First (User Stories 1, 2, 3)

1. Complete Phase 1: Setup
2. Complete Phase 2: Sign Up (US1)
3. Complete Phase 3: Sign In (US2)
4. Complete Phase 4: Session Persistence (US3)
5. Complete Phase 5: Navigation Integration
6. **STOP and VALIDATE**: Test core auth flow
7. Deploy/demo if ready

### Full Implementation

1. MVP phases → Core auth works
2. Phase 6: Device Account Migration (US4)
3. Phase 7: Polish and Testing

---

## Notes

- User Stories 1, 2, 3 are all P1 - implement together for MVP
- User Story 4 (device migration) is P2 - can be deferred
- Session tokens never expire per clarification
- Single AuthPage with toggle for Sign In / Sign Up per UX decision
