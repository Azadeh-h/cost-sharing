# AGENTS.md — Cost Sharing App

## What This Repo Is

A .NET MAUI Android app for tracking shared expenses and settling debts among groups. Local-first with SQLite storage, Google OAuth for identity, MVVM architecture.

> **Important**: The `specs/001-cost-sharing-app/` folder describes an aspirational full-stack architecture (backend API + React + Google Drive). The **actual implementation** is a standalone .NET MAUI mobile app with local SQLite only. The harness reflects the real codebase, not the specs.

## Quick Start (Agent Bootstrap)

```bash
# 1. Verify .NET 9 SDK is installed
dotnet --version  # expecting 9.x

# 2. Run full validation (build + 88 tests)
cd CostSharingApp && dotnet test tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj

# 3. Expected output: Passed! Failed: 0, Passed: 88
```

**Time to healthy**: ~10 seconds from clone.

## Project Structure

```
CostSharingApp/
├── src/
│   ├── CostSharing.Core/          # Core business logic (net9.0)
│   │   ├── Models/                 # Domain models
│   │   ├── Interfaces/             # Service contracts
│   │   ├── Services/               # Business services
│   │   └── Algorithms/             # Debt simplification (Min-Cash-Flow)
│   └── CostSharingApp/             # MAUI app (net9.0-android)
│       ├── Views/                  # XAML pages
│       ├── ViewModels/             # MVVM ViewModels
│       ├── Services/               # App services (Auth, Expense, Group, Sync)
│       ├── Platforms/Android/      # Android-specific code
│       └── Resources/              # Images, styles, fonts
├── tests/
│   └── CostSharingApp.Tests/      # 88 xUnit tests (net9.0)
└── specs/                          # Requirements docs (aspirational — see note above)
```

## Key Commands

| Command | What It Does |
|---------|-------------|
| `just validate` | Quick health check: build + 88 tests |
| `just check` | Full health check: restore + build + test |
| `just build-app` | Build full MAUI Android APK |
| `just test-log` | Run tests with trx evidence output |
| `just setup-emulator` | Create Android AVD (one-time) |
| `just boot-emulator` | Boot headless emulator (quick boot) |
| `just launch-app` | Build + deploy + launch app on emulator |
| `just screenshot "label"` | Capture emulator screenshot |
| `just smoke-test` | ADB-scripted navigation + screenshots |
| `just stop-emulator` | Graceful shutdown (saves snapshot) |

## Architecture Decisions

- **MVVM**: ViewModels use `CommunityToolkit.Mvvm` with `[ObservableProperty]` and `[RelayCommand]`
- **DI**: All services registered in `MauiProgram.cs`
- **Storage**: SQLite via `sqlite-net-pcl` — no backend API
- **Auth**: Google OAuth — platform-specific implementations in `Platforms/`
- **Algorithms**: Min-Cash-Flow for debt simplification (well-tested, 11 dedicated tests)

## Test Coverage

Tests cover core algorithms only (not UI or services):
- Split calculation (16 tests)
- Debt calculation (13 tests)
- Debt simplification / Min-Cash-Flow (11 tests)
- Plus additional model and service tests

## Working Agreements

1. **Run tests before and after every change** — `dotnet test` is the source of truth
2. **No secrets in code** — OAuth client IDs are in `appsettings.json` (gitignored for production)
3. **Follow existing patterns** — MVVM, DI registration, StyleCop conventions
4. **Log difficulties** — see `docs/project-rules/difficulty-ledger.md`

## Harness

See `docs/project-rules/harness.md` for the full agent harness specification (when it exists). The harness enables automated build → test → observe loops.

## Difficulty Ledger

See `docs/project-rules/difficulty-ledger.md`. Every time you hit friction — confusing errors, missing tools, undocumented gotchas — log it there. Then fix it. Encoded fixes compound velocity.
