# Implementation Plan: Cost-Sharing Application

**Branch**: `001-cost-sharing-app` | **Date**: 2026-01-05 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-cost-sharing-app/spec.md`

**⚠️ IMPLEMENTATION NOTE**: This document reflects the original architecture design with Google Drive synchronization. The actual implementation uses **local SQLite storage only** - Google Drive integration was removed during development. This document is preserved for historical reference.

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a **local-first, cross-platform .NET MAUI application** for collaborative cost-sharing. Users install native apps on Windows, macOS, Android, and iOS. The app connects directly to Google Drive for data storage and synchronization (no backend server required). Users create groups, invite members via email/SMS (using SendGrid/Twilio SDKs), add expenses with flexible splitting options, and view simplified debt settlements. The application is distributed via direct download (.exe, .apk, .dmg, .ipa) from website/GitHub releases, eliminating hosting costs entirely.

**Key Architectural Decision**: This is a **peer-to-peer, serverless architecture** where each app instance is autonomous and syncs via Google Drive shared folders.

## Technical Context
Framework**: .NET MAUI (Multi-platform App UI) for cross-platform native apps  
**Primary Dependencies**: 
- Google.Apis.Drive.v3 (direct Google Drive access)
- SendGrid SDK (in-app email sending)
- Twilio SDK (in-app SMS sending)
- CommunityToolkit.Mvvm (MVVM pattern)
- SQLite (local caching/offline support)

**Storage**: 
- Primary: File-based JSON in Google Drive (user's own Drive, shared folders for groups)
- Local: SQLite cache for offline operation
- Sync: Optimistic concurrency with conflict resolution

**Testing**: xUnit (business logic), xUnit.Device (UI testing on devices)  
**Target Platforms**: 
- Windows 10/11 (.exe installer)
- macOS 11+ (.dmg installer)
- Android 8.0+ (.apk)
- iOS 13+ (.ipa via TestFlight/direct)

**Project Type**: Cross-platform native mobile + desktop application (single codebase)  
**Performance Goals**: 
- App launch: <3s cold start
- Sync operations: <5s for typical group
- Offline-capable: Full functionality without internet
- Google Drive sync when online

**Constraints**: 
- No backend server (zero hosting costs)
- Google Drive API rate limits (1000 requests/100s per user)
- Local storage + sync architecture
- Each user authenticates with their own Google account

**Scale/Scope**: 
- Small-to-medium groups (2-50 members)
- 10-100 expenses per group
- Optimized for offline-first operation
- Peer-to-peer collaboration via shared Google Drive folders

**Distribution**: Direct download from website/GitHub releases (no app store initially)
**Constraints**: Google Drive API rate limits (1000 requests/100s per user), file locking for concurrent access, <200ms API response time p95  
**Scale/Scope**: MVP for small-to-medium groups (2-50 members), ~10-20 expenses per group, extensible architecture for scaling

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Component-Based Architecture
**Status**: ✅ PASS  
**Evaluation**: .NET MAUI uses MVVM pattern with Views, ViewModels, and Models as distinct components. Each user story maps to Views (GroupPage, ExpensePage) and ViewModels (GroupViewModel, ExpenseViewModel) with clear separation. Components are testable in isolation.

### Principle II: Code Quality & Linting (NON-NEGOTIABLE)
**Status**: ✅ PASS  
**Evaluation**: .NET MAUI projects use .editorconfig, Roslyn analyzers, and StyleCop for C#/XAML. Linting integrated into build pipeline.

### Principle III: Separation of Concerns
**Status**: ✅ PASS  
**Evaluation**: Clear MVVM separation: Views (UI/XAML), ViewModels (presentation logic), Models (domain entities), Services (business logic, data access). No backend mixing concerns since there is no backend.

### Principle IV: Testing & Quality Gates
**Status**: ✅ PASS  
**Evaluation**: xUnit for unit tests, xUnit.Device for UI testing on actual devices. Debt calculation and sync logic will have ≥80% coverage.

### Principle V: Maintainability & Documentation
**Status**: ✅ PASS  
**Evaluation**: MAUI project structure is well-documented. XML docs for public APIs, architecture decision records for local-first + Google Drive sync approach.

### Gate Evaluation

**Overall**: ✅ **PASS** - All constitutional principles satisfied with .NET MAUI architecture.

**Note**: Simplified architecture (no backend) actually improves constitutional compliance by reducing complexity and maintaining clearer boundaries.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── CostSharing.API/              # ASP.NET Core Web API project
CostSharingApp/                       # .NET MAUI solution
├── src/
│   ├── CostSharingApp/              # Main MAUI project
│   │   ├── Platforms/               # Platform-specific code
│   │   │   ├── Android/             # Android-specific
│   │   │   ├── iOS/                 # iOS-specific
│   │   │   ├── Windows/             # Windows-specific
│   │   │   └── MacCatalyst/         # macOS-specific
│   │   ├── Views/                   # XAML pages
│   │   │   ├── Groups/              # Group pages
│   │   │   ├── Expenses/            # Expense pages
│   │   │   ├── Members/             # Member pages
│   │   │   └── Debts/               # Debt pages
│   │   ├── ViewModels/              # MVVM ViewModels
│   │   │   ├── Groups/
│   │   │   ├── Expenses/
│   │   │   ├── Members/
│   │   │   └── Debts/
│   │   ├── Models/                  # Domain entities
│   │   ├── Services/                # Business logic
│   │   │   ├── DriveService.cs      # Google Drive sync
│   │   │   ├── DebtService.cs       # Debt calculations
│   │   │   ├── NotificationService.cs # SendGrid/Twilio
│   │   │   └── CacheService.cs      # Local SQLite cache
│   │   ├── Helpers/                 # Utilities
│   │   ├── Resources/               # Images, styles, strings
│   │   ├── App.xaml                 # App entry point
│   │   └── MauiProgram.cs           # Dependency injection setup
│   └── CostSharing.Core/            # Shared business logic library
│       ├── Models/                  # Domain entities (shared)
│       ├── Services/                # Business services
│       ├── Algorithms/              # Debt simplification
│       └── Validation/              # Validation rules
├── tests/
│   ├── CostSharingApp.Tests/        # Unit tests (xUnit)
│   └── CostSharingApp.UITests/      # UI tests (xUnit.Device)
├── docs/                            # Documentation
└── build/                           # Build scripts and CI/CD

.editorconfig                        # Code style rules
.github/
└── workflows/                       # CI/CD pipelines for building installers
```

**Structure Decision**: .NET MAUI single-project structure with platform-specific folders. MVVM pattern with Views, ViewModels, and Services. Core business logic in separate library for reusability and testability. No backend project since there is no server
*Re-evaluation after Phase 1 design completion*

**No violations identified.** All design decisions align with constitutional principles. Google Drive storage approach, while unconventional, is a user requirement (not a constitutional violation) and is fully documented in research.md with mitigation strategies for concurrency and performance concerns.
**Status**: ✅ PASS  
**Post-Design Validation**: Data model and API contracts support component isolation. Each entity maps to distinct services and components (GroupService→GroupCard, ExpenseService→ExpenseForm, etc.). React components designed with clear props interfaces matching DTOs.

### Principle II: Code Quality & Linting (NON-NEGOTIABLE)
**Status**: ✅ PASS  
**Post-Design Validation**: Project structure includes .editorconfig at root, linting configuration verified in quickstart.md. CI/CD integration planned for automated quality gates.

### Principle III: Separation of Concerns
**Status**: ✅ PASS  
**Post-Design Validation**: Clean architecture enforced: Core (business logic), Infrastructure (I/O), API (presentation), Shared (contracts). API contracts define clear boundaries between frontend and backend. No cross-layer violations in design.

### Principle IV: Testing & Quality Gates
**Status**: ✅ PASS  
**Post-Design Validation**: Test structure defined for all layers. Critical algorithms (debt simplification) identified for ≥80% coverage requirement. E2E test strategy using Playwright ensures user story validation.

### Principle V: Maintainability & Documentation
**Status**: ✅ PASS  
**Post-Design Validation**: Comprehensive documentation delivered: research.md (decisions), data-model.md (entities), api-spec.yaml (contracts), quickstart.md (onboarding). Architecture decisions explicitly documented with rationale.

### Final Gate Evaluation

**Overall**: ✅ **PASS** - Design phase maintains constitutional compliance. All principles satisfied with concrete implementation guidance.

**Architectural Decision Record**: Google Drive file storage approach documented in research.md with concurrency strategy, performance considerations, and trade-offs explicitly stated. Decision justified by user requirements and documented for future reference.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
