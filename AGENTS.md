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
| `just validate` | Quick health check: build + 88 tests (self-heals stale builds) |
| `just check` | Full health check: restore + build + test (self-heals NuGet corruption) |
| `just doctor` | Diagnose full environment: .NET, SDK, emulator, AVD, ADB, NuGet |
| `just review` | AI code review sensor: semantic review of changed files (30s timeout, non-blocking) |
| `just build-app` | Build full MAUI Android APK |
| `just test-log` | Run tests with trx evidence output |
| `just setup-emulator` | Create Android AVD (auto-installs missing system image) |
| `just boot-emulator` | Boot headless emulator (auto-kills orphan emulators) |
| `just launch-app` | Build + deploy + launch app (retries on crash) |
| `just screenshot "label"` | Capture emulator screenshot |
| `just smoke-test` | ADB-scripted navigation + screenshots |
| `just stop-emulator` | Graceful shutdown (saves snapshot) |
| `just mutate` | Run Stryker.NET mutation testing against CostSharing.Core (baseline: 64.34%) |
| `just drift` | Run 4 drift sensors: dependencies, coverage, dead code, doc drift (JSON report) |

### Exit Codes

All self-healing recipes use structured exit codes:
- **0**: Clean success
- **1**: Unrecoverable failure
- **2**: Self-healed and succeeded (check `⚠️ Self-healed:` messages in output)

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

## Mutation Testing

Run `just mutate` to measure test effectiveness via Stryker.NET. Mutation testing introduces small code changes (mutants) and checks whether tests catch them.

- **Baseline score**: 64.34% (2026-05-13)
- **Thresholds**: break=60%, low=70%, high=80%
- **Per-file scores**:
  - SplitCalculationService: 90.91% 🟢
  - DebtSimplificationAlgorithm: 61.76% 🟡
  - DebtCalculationService: 53.95% 🔴
- **Interpreting results**: Surviving mutants indicate assertion gaps — tests pass despite meaningful code changes. Focus on killed/survived ratio per file. A low score means tests need stronger assertions, not that production code is wrong.
- **Config**: `CostSharingApp/stryker-config.json`
- **Reports**: Generated locally in `StrykerOutput/` (gitignored)

## Drift Detection

Run `just drift` to scan for gradual codebase degradation across 4 sensors:

- **Dependencies**: Flags outdated and vulnerable NuGet packages (`dotnet list package --outdated/--vulnerable`)
- **Coverage**: Per-file line coverage for CostSharing.Core via coverlet (baseline: 79%)
- **Dead Code**: Identifies unreferenced interfaces in `CostSharing.Core/Interfaces/` (DI-registered interfaces excluded from "dead" status but flagged if untested)
- **Doc Drift**: Checks documented facts (test count, command count) in AGENTS.md and harness.md against reality

**Output**: Structured JSON to stdout with per-sensor status (✅ healthy / ⚠️ drifting / ❌ critical) and overall verdict.

**Interpreting results**: Each sensor runs independently. "Drifting" means gradual degradation detected — not blocking but worth attention. "Critical" means significant mismatch between documented and actual state.

## Working Agreements

1. **Run tests before and after every change** — `dotnet test` is the source of truth
2. **No secrets in code** — OAuth client IDs are in `appsettings.json` (gitignored for production)
3. **Follow existing patterns** — MVVM, DI registration, StyleCop conventions
4. **Log difficulties** — see `docs/project-rules/difficulty-ledger.md`

## Harness

See `docs/project-rules/harness.md` for the full agent harness specification (when it exists). The harness enables automated build → test → observe loops.

## Difficulty Ledger

See `docs/project-rules/difficulty-ledger.md`. Every time you hit friction — confusing errors, missing tools, undocumented gotchas — log it there. Then fix it. Encoded fixes compound velocity.


## Execution Loop (Mandatory)

For any code change:

1. Identify minimal set of files to modify
2. Apply the smallest possible change
3. Run `just validate`
4. If validation passes → STOP
5. If validation fails:
   - Inspect failure output
   - Retry ONCE with minimal fix
6. If still failing → STOP and summarize root cause

Never perform more than 2 modification attempts per task.

## Allowed Write Scope

Agents may ONLY modify:
- CostSharingApp/src/CostSharing.Core/**
- CostSharingApp/tests/CostSharingApp.Tests/**

## Restricted Areas

Do NOT modify unless explicitly instructed:
- Views/
- Platforms/Android/
- Resources/
- build / publish / keystore config


## Domain Invariants (Must Always Hold)

- Total expense = sum of all participant splits
- Debt simplification must preserve net balances
- Settlement must reduce outstanding balances
- No negative or “phantom” debt creation
- Decimal calculations must be deterministic

If these are violated → task is FAILED even if tests pass



## Task Output (Required)

Each task must output:

- Files changed
- Commands executed
- Test result (pass/fail)
- Retry count
- Final status

Example:
{
  "files": ["SplitCalculationService.cs"],
  "commands": ["just validate"],
  "result": "pass",
  "retries": 1
}



## Stop Conditions

The agent must stop and request human input if:

- Changes affect more than 5 files
- Tests fail after 2 attempts
- Domain invariants are unclear
- Changes involve platform-specific code

Do not continue autonomously in these cases.


## Multi-Agent Flow

Default execution order for non-trivial tasks:
1. planner
2. implementer
3. verifier
4. reviewer (automatic — inferential feedback sensor)
5. auditor (only for financial/domain logic changes)

The verifier must approve before a task is considered complete.
The reviewer runs automatically after verification — non-blocking in v1 (warns but doesn't fail).
The auditor is required for changes involving split calculation, debt calculation, settlements, or Min-Cash-Flow logic.
