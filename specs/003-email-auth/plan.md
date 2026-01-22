# Implementation Plan: Email-Based Authentication

**Branch**: `003-email-auth` | **Date**: 2026-01-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-email-auth/spec.md`

## Summary

Implement email-based authentication to replace the current device-based auto-login (`@device.local`). Users will see a welcome page on first launch with options to Sign Up or Sign In. Sessions persist indefinitely until explicit sign out. Existing device accounts can optionally migrate to email-based accounts.

**Key Architectural Decision**: Single authentication page with toggle between Sign In and Sign Up views. Session tokens stored in SecureStorage for persistence across app restarts.

## Technical Context

**Framework**: .NET MAUI (existing application)  
**Primary Dependencies**: 
- Existing CostSharing.Core library (Models, Services)
- Existing CostSharingApp (ViewModels, Views, Services)
- SQLite (local storage via CacheService)
- SecureStorage (MAUI secure storage for session tokens)
- Existing AuthService (will be enhanced)

**Storage**: 
- Local: SQLite database (User entity)
- Secure: MAUI SecureStorage for session persistence

**Testing**: xUnit for unit tests, manual testing for UI flows  
**Target Platforms**: Android, iOS, Windows, macOS

**Project Type**: Feature addition to existing .NET MAUI application  

**Constraints**: 
- Must maintain backward compatibility with existing users
- Sessions never expire (user stays signed in until explicit sign out)
- Local-only authentication (no external auth providers)
- Must preserve existing groups/expenses when migrating device accounts

## Constitution Check

### Principle I: Component-Based Architecture
**Status**: ✅ PASS  
**Evaluation**: New feature follows existing MVVM pattern. AuthPage with AuthViewModel handles both sign in/sign up flows.

### Principle II: Code Quality & Linting
**Status**: ✅ PASS  
**Evaluation**: Uses existing .editorconfig and StyleCop analyzers.

### Principle III: Separation of Concerns
**Status**: ✅ PASS  
**Evaluation**: AuthService handles business logic, AuthViewModel handles UI state, AuthPage handles presentation.

### Principle IV: Testing & Quality Gates
**Status**: ✅ PASS  
**Evaluation**: Unit tests for validation logic and session persistence.

### Principle V: Maintainability & Documentation
**Status**: ✅ PASS  
**Evaluation**: XML docs for new public APIs.

### Gate Evaluation
**Overall**: ✅ **PASS**

## Project Structure

### New Files (this feature)

```text
CostSharingApp/src/CostSharingApp/
├── Views/
│   └── Auth/
│       └── AuthPage.xaml              # Combined Sign In / Sign Up page
│       └── AuthPage.xaml.cs
├── ViewModels/
│   └── Auth/
│       └── AuthViewModel.cs           # Authentication view model
└── Services/
    └── SessionService.cs              # Session persistence service
```

### Modified Files

```text
CostSharingApp/src/CostSharingApp/
├── App.xaml.cs                        # Check auth state on startup
├── AppShell.xaml                      # Add auth page route
├── AppShell.xaml.cs                   # Register auth page
├── Services/
│   └── AuthService.cs                 # Add session persistence, validation
├── MauiProgram.cs                     # Register new services
CostSharingApp/src/CostSharing.Core/
├── Models/
│   └── User.cs                        # Add AccountType property
```

## Dependencies

This feature depends on:
- Existing User model (will add AccountType field)
- Existing AuthService (will enhance with session persistence)
- Existing CacheService for SQLite storage
- SecureStorage for session token persistence

## Implementation Phases

### Phase 1: Setup & Core Services
- Create SessionService for session persistence
- Add AccountType enum and property to User model
- Enhance AuthService with session token generation and validation

### Phase 2: Authentication UI
- Create AuthPage with Sign In / Sign Up toggle
- Create AuthViewModel with form validation
- Add email format and password strength validation

### Phase 3: App Flow Integration
- Modify App.xaml.cs to check auth state on startup
- Route to AuthPage if not authenticated, Dashboard if authenticated
- Register routes in AppShell

### Phase 4: Session Persistence
- Store session token in SecureStorage on successful login
- Restore session on app startup
- Clear session on explicit sign out

### Phase 5: Device Account Migration (P2)
- Add "Add Email to Account" option in profile settings
- Allow device accounts to set email and password
- Preserve all existing data during migration

### Phase 6: Testing & Polish
- Unit tests for validation logic
- Manual testing of auth flows
- Error handling and user feedback
