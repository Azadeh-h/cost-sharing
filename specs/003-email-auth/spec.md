# Feature Specification: Email-Based Authentication

**Feature Branch**: `003-email-auth`  
**Created**: 2026-01-22  
**Status**: Draft  
**Input**: User description: "I want to be able to have my own email as a username instead of device.local. Create a sign up/in page"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign Up with Email (Priority: P1)

As a new user, I want to create an account using my email address and a password so that I have a personal identity in the app instead of an anonymous device ID.

**Why this priority**: This is the core feature - without sign up, users cannot have email-based accounts. It replaces the automatic device-based account creation.

**Independent Test**: Can be fully tested by launching the app, seeing the sign up page, entering email/password/name, and verifying the account is created and user is logged in.

**Acceptance Scenarios**:

1. **Given** I am a new user opening the app for the first time, **When** the app launches, **Then** I see a welcome screen with options to Sign Up or Sign In.

2. **Given** I am on the welcome screen, **When** I tap "Sign Up", **Then** I see a sign up form with fields for email, password, confirm password, and display name.

3. **Given** I am on the sign up form, **When** I enter a valid email, a password that meets requirements, confirm the password, and enter my name, **Then** I can tap "Create Account" to complete registration.

4. **Given** I have entered valid sign up information, **When** I tap "Create Account", **Then** my account is created and I am taken to the main dashboard.

5. **Given** I try to sign up with an email that already exists, **When** I tap "Create Account", **Then** I see an error message indicating the email is already registered.

---

### User Story 2 - Sign In with Email (Priority: P1)

As a returning user, I want to sign in with my email and password so that I can access my groups and expenses.

**Why this priority**: Without sign in, returning users cannot access their accounts. This is equally important as sign up.

**Independent Test**: Can be tested by creating an account, signing out, and then signing back in with the same credentials.

**Acceptance Scenarios**:

1. **Given** I am on the welcome screen, **When** I tap "Sign In", **Then** I see a sign in form with fields for email and password.

2. **Given** I am on the sign in form with valid credentials, **When** I enter my email and password and tap "Sign In", **Then** I am logged in and taken to the main dashboard.

3. **Given** I enter an incorrect password, **When** I tap "Sign In", **Then** I see an error message indicating invalid credentials.

4. **Given** I enter an email that doesn't exist, **When** I tap "Sign In", **Then** I see an error message indicating the account was not found.

---

### User Story 3 - Stay Signed In (Priority: P1)

As a user, I want to stay signed in after closing the app so that I don't have to enter my credentials every time I use the app.

**Why this priority**: User experience is critical - requiring sign in on every app launch would be frustrating.

**Independent Test**: Can be tested by signing in, closing the app, reopening it, and verifying the user is still logged in.

**Acceptance Scenarios**:

1. **Given** I am signed in and close the app, **When** I reopen the app, **Then** I am still signed in and go directly to the dashboard.

2. **Given** I am signed in, **When** I explicitly sign out from settings, **Then** I am taken to the welcome screen and must sign in again.

---

### User Story 4 - Migrate Device Account to Email (Priority: P2)

As an existing user with a device-based account (@device.local), I want to upgrade my account to use my real email so that I can access my data from any device.

**Why this priority**: Important for existing users but can be deferred since it's a migration path, not core functionality.

**Independent Test**: Can be tested by using the app with a device account, then going through the migration flow to add a real email.

**Acceptance Scenarios**:

1. **Given** I have an existing device-based account with groups and expenses, **When** I access profile settings, **Then** I see an option to "Add Email to Account".

2. **Given** I tap "Add Email to Account", **When** I enter my email and set a password, **Then** my account is updated with the real email and password.

3. **Given** I have migrated to email, **When** I sign in on another device with my email, **Then** I can access my synced groups and expenses.

---

### Edge Cases

- What happens if the user loses internet during sign up? Show appropriate error and allow retry; form data is preserved.
- What happens if the password doesn't meet requirements? Show inline validation error before submission.
- What happens if user enters mismatched passwords in confirm field? Show error indicating passwords don't match.
- What happens if session expires? Redirect to sign in page with message "Please sign in again".
- What happens if user tries to sign up with an invalid email format? Show validation error with proper email format hint.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a welcome/landing page when user is not signed in
- **FR-002**: System MUST provide a Sign Up form with email, password, confirm password, and display name fields
- **FR-003**: System MUST provide a Sign In form with email and password fields
- **FR-004**: System MUST validate email format before allowing submission
- **FR-005**: System MUST require passwords to be at least 8 characters with at least one number
- **FR-006**: System MUST verify password confirmation matches password
- **FR-007**: System MUST prevent duplicate email registrations
- **FR-008**: System MUST securely store user credentials locally (hashed passwords)
- **FR-009**: System MUST persist user session so they stay signed in after closing the app
- **FR-010**: System MUST provide a Sign Out option in settings
- **FR-011**: System MUST show appropriate error messages for all failure cases
- **FR-012**: System MUST redirect unauthenticated users to the welcome page
- **FR-013**: System MUST allow existing device-based accounts to add a real email address
- **FR-014**: System MUST display the user's email in the profile/settings section

### Key Entities

- **User**: Enhanced to differentiate between device-based accounts (@device.local) and email-based accounts. Key attributes: Email, PasswordHash, Name, IsEmailVerified, AccountType (device/email)
- **Session**: Represents the persisted login state. Key attributes: UserId, CreatedAt, ExpiresAt (optional for "remember me")

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete sign up in under 1 minute
- **SC-002**: Users can complete sign in in under 30 seconds
- **SC-003**: 100% of returning users remain signed in after app restart (unless explicitly signed out)
- **SC-004**: 0% of users accidentally lose access to their groups when switching from device to email account
- **SC-005**: All password validation errors are shown inline before form submission
- **SC-006**: Email format validation prevents 100% of invalid email submissions

## Assumptions

- Users have valid email addresses they can remember
- Password requirements (8+ characters with at least one number) are acceptable for the user base
- Local storage (SQLite + SecureStorage) is sufficient for credential persistence
- Email verification is not required for initial sign up (can be added later)
- The existing device-based account flow will be replaced, not run in parallel
- Users upgrading from device accounts will retain all their existing groups and expenses
- Sessions never expire - users stay signed in indefinitely until explicit sign out

## Clarifications

### Session 2026-01-22

- Q: Should sessions expire after a period of time or inactivity? → A: Sessions never expire - user stays signed in until they explicitly sign out
