# Implementation Plan: Gmail Invitation & Member Sync

**Branch**: `004-gmail-invite-sync` | **Date**: 2026-01-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-gmail-invite-sync/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Enable Gmail-based invitations for group members with automatic membership linking when invited users sign in with matching email addresses. Leverages existing `IGmailInvitationService` interface and `GroupMember` model. Adds `PendingInvitation` entity for tracking pre-signup invitations.

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: .NET MAUI, CommunityToolkit.Mvvm 8.4.0, Google.Apis.Gmail.v1 1.73.0, sqlite-net-pcl 1.9.172  
**Storage**: SQLite (local), SecureStorage (tokens), Google Drive (cloud sync)  
**Testing**: xUnit (CostSharingApp.Tests project)  
**Target Platform**: Android 21+, iOS 15+, macOS Catalyst, Windows 10+  
**Project Type**: Mobile + Desktop MAUI app  
**Performance Goals**: Email sending < 3 seconds, membership linking instant on sign-in  
**Constraints**: Offline-capable (queue invitations), Gmail OAuth required for sending  
**Scale/Scope**: Single user device, ~10 groups, ~100 members total

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Component-Based Architecture | ✅ PASS | New `InvitationLinkingService` self-contained with `IInvitationLinkingService` interface; `PendingInvitation` entity isolated |
| II. Code Quality & Linting | ✅ PASS | Will follow existing StyleCop/Roslyn analyzer rules; all new code must pass linting |
| III. Separation of Concerns | ✅ PASS | Service layer handles business logic; ViewModel handles UI; Models have no logic |
| IV. Testing & Quality Gates | ✅ PASS | Unit tests for invitation service; integration tests for email matching |
| V. Maintainability & Documentation | ✅ PASS | XML documentation on all public APIs; quickstart.md generated |

**Pre-Design Gate**: ✅ ALL GATES PASS - Proceeded to Phase 0

### Post-Design Re-evaluation

| Principle | Status | Verification |
|-----------|--------|--------------|
| I. Component-Based Architecture | ✅ PASS | `IInvitationLinkingService` interface defined in [service-contracts.md](contracts/service-contracts.md); clear inputs/outputs |
| II. Code Quality & Linting | ✅ PASS | All code samples follow StyleCop conventions; linting rules unchanged |
| III. Separation of Concerns | ✅ PASS | `InvitationLinkingService` in Core; UI logic in ViewModels; email sending in existing `GmailInvitationService` |
| IV. Testing & Quality Gates | ✅ PASS | Test cases defined in [quickstart.md](quickstart.md); unit test file planned |
| V. Maintainability & Documentation | ✅ PASS | XML docs specified; quickstart.md, data-model.md, and contracts generated |

**Post-Design Gate**: ✅ ALL GATES PASS - Ready for Phase 2 (tasks)

## Project Structure

### Documentation (this feature)

```text
specs/004-gmail-invite-sync/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
CostSharingApp/src/
├── CostSharing.Core/
│   ├── Interfaces/
│   │   ├── IGmailInvitationService.cs  # Existing - sends emails
│   │   └── IInvitationLinkingService.cs # New - links memberships
│   ├── Models/
│   │   ├── User.cs          # Existing - email matching
│   │   ├── Group.cs         # Existing
│   │   ├── GroupMember.cs   # Existing - junction table
│   │   └── PendingInvitation.cs # New - tracks pre-signup invites
│   └── Services/
│       └── InvitationLinkingService.cs # New - email matching logic
├── CostSharingApp/
│   ├── Services/
│   │   └── GmailInvitationService.cs # Existing - implements interface
│   ├── ViewModels/
│   │   └── GroupMemberViewModel.cs   # Modified - add invitation flow
│   └── Views/
│       └── GroupMemberPage.xaml      # Modified - invitation UI

CostSharingApp/tests/
├── CostSharingApp.Tests/
│   └── Services/
│       └── InvitationLinkingServiceTests.cs # New - unit tests
```

**Structure Decision**: Mobile + API structure with .NET MAUI app. Core logic in `CostSharing.Core`, platform-specific implementations in `CostSharingApp`.

## Complexity Tracking

> No Constitution violations - table not required.
