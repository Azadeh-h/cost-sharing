# L4 Self-Healing Harness

**Mode**: Simple

ℹ️ Builds on L3 harness (emulator lifecycle) from plan `001-harness-l3`.

## Summary

Upgrade the agent harness from L3 (emulator + UI evidence) to L4 (self-healing). Every recipe that can fail due to environment drift should detect the problem and fix it automatically — or fail with a clear, actionable message. Agents should never get stuck debugging stale builds, corrupted NuGet caches, orphan emulator processes, or unregistered SDK packages.

**WHAT**: Add auto-recovery logic to all justfile recipes so they self-heal from common environment failures.
**WHY**: L3 proved that emulator setup is fragile (we hit SDK registration issues, monkey exit-code failures). Agents waste time diagnosing environment problems instead of shipping features. L4 eliminates this class of friction entirely.

## Goals

- **G1**: `just validate` self-heals from stale/corrupted builds (auto-clean + rebuild)
- **G2**: `just check` detects and recovers from NuGet cache corruption (auto-restore with `--force-evaluate`)
- **G3**: `just boot-emulator` kills orphan emulator/ADB processes before booting
- **G4**: `just setup-emulator` auto-installs missing system images via `sdkmanager`
- **G5**: `just launch-app` detects app crash on launch and retries with a clean deploy
- **G6**: All recipes produce structured exit codes (0=success, 1=unrecoverable, 2=recovered-and-succeeded)
- **G7**: A new `just doctor` recipe diagnoses the full environment and reports status

## Non-Goals

- CI/CD pipeline integration (future work)
- Pre-commit hooks
- Automatic SDK/JDK installation from scratch (assume SDK exists, just may need package updates)
- Retry loops for network failures during `sdkmanager` downloads
- Watchdog/daemon that monitors emulator health continuously

## Target Domains

| Domain | Status | Relationship | Role in This Feature |
|--------|--------|-------------|---------------------|
| _harness | existing | **modify** | All changes are to justfile + harness.md + AGENTS.md |

No new domains. This is purely infrastructure — all changes live in the harness.

## Complexity

- **Score**: CS-2 (small)
- **Breakdown**: S=1, I=0, D=0, N=1, F=0, T=1
  - S=1: Multiple recipes in justfile, plus harness.md and AGENTS.md updates
  - I=0: All internal tooling, no new external deps
  - D=0: No data/state changes
  - N=1: Some discovery needed around exact recovery commands and edge cases
  - F=0: Standard requirements
  - T=1: Manual verification via live emulator testing
- **Confidence**: 0.80
- **Assumptions**:
  - Android SDK is installed at `ANDROID_HOME` or `~/Library/Android/sdk`
  - `sdkmanager` and `avdmanager` are on PATH or at known SDK locations
  - macOS/Apple Silicon (arm64) — `sdkmanager` packages use `arm64-v8a`
- **Dependencies**: L3 harness (complete)
- **Risks**: Shell scripting complexity — each recipe's recovery logic must not mask real failures
- **Phases**: Single phase — all self-healing improvements in one pass

## Acceptance Criteria

- **AC1**: `just validate` succeeds after manually corrupting `bin/` and `obj/` directories (auto-clean triggered)
- **AC2**: `just check` succeeds after deleting a NuGet package from local cache (auto-restore triggered)
- **AC3**: `just boot-emulator` succeeds when an orphan emulator process exists from a previous crash (auto-kill triggered)
- **AC4**: `just setup-emulator` succeeds when the system image is not registered with sdkmanager (auto-install triggered)
- **AC5**: `just launch-app` retries once if the app crashes within 3 seconds of launch
- **AC6**: `just doctor` reports status of: .NET SDK, Android SDK, emulator binary, AVD, ADB, system image, NuGet restore — and suggests fix commands for any issues found
- **AC7**: All self-healing actions emit a `⚠️ Self-healed: <description>` message so agents know recovery happened
- **AC8**: Exit code 2 when a recipe self-healed, exit code 0 when clean success, exit code 1 when unrecoverable

## Risks & Assumptions

- **R1**: Recovery logic that's too aggressive could mask real build errors (mitigation: only retry on specific known failure patterns)
- **R2**: `sdkmanager` auto-install requires network and license acceptance (mitigation: `--licenses` flag, fail gracefully if offline)
- **R3**: Killing orphan processes risks killing a user's intentional emulator session (mitigation: only kill emulators matching our AVD name)
- **A1**: The machine has internet access for `sdkmanager` downloads
- **A2**: The agent has write permissions to `ANDROID_HOME`

## Testing Strategy

- **Approach**: Lightweight
- **Rationale**: Shell script changes — verify by running recipes with simple assertion checks
- **Focus Areas**: Each self-healing path exercised at least once (corrupt build, orphan emulator, missing SDK package)
- **Excluded**: No unit test framework — shell-level checks only
- **Mock Usage**: N/A — all tests are against real environment

## Documentation Strategy

- **Location**: Update `harness.md` and `AGENTS.md` only
- **Rationale**: Internal infrastructure — no user-facing docs needed

## Open Questions

All resolved — see Clarifications below.

## Workshop Opportunities

| Topic | Type | Why Workshop | Key Questions |
|-------|------|--------------|---------------|
| Failure Detection Patterns | CLI Flow | Each recipe needs specific failure signatures to detect vs. generic errors | What stdout/stderr patterns indicate recoverable vs. unrecoverable failures for dotnet build, NuGet restore, emulator boot? |

## Clarifications

### Session 2026-05-11

| # | Question | Answer |
|---|----------|--------|
| Q1 | Workflow Mode | **Simple** — single phase, inline tasks |
| Q2 | Testing Strategy | **Lightweight** — simple shell-level assertion checks |
| Q3 | `just doctor` behavior | **Report + offer fix commands** — show status and suggest copy-paste fix commands, but don't auto-fix |
| Q4 | Self-healing opt-out | **No opt-out** — self-healing always on, keep it simple |
| Q5 | Documentation Strategy | **No new docs** — update harness.md and AGENTS.md only |
| Q6 | Structured exit codes | **Yes** — exit 0 (clean), exit 1 (unrecoverable), exit 2 (self-healed) |
