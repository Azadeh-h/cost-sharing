# Agent Harness

**Version**: 2.0.0
**Created**: 2026-05-11
**Maturity Level**: L4
**Project Type**: mobile (.NET MAUI — Android only)

## Purpose
Enable agents to iterate on the CostSharingApp — a .NET MAUI mobile app with local SQLite storage — using a build → test → observe cycle in 30–60 second loops. At L4, the harness self-heals from common environment failures (stale builds, NuGet corruption, orphan emulators, missing SDK packages) so agents never get stuck debugging infrastructure.

## Boot
- **Command**: `just check` (or `just validate` for quick runs)
- **Health Check**: `just test`
- **Expected Response**: `Passed!` (exit code 0)
- **Boot Time**: ~10-30s (build + test)
- **Known Issue**: N/A — Android-only build has no Xcode dependency.
- **Idempotent**: Yes — rebuild is safe; `dotnet build` is incremental

> **Note**: This is a mobile app with no backend server. There is no HTTP health endpoint.
> The "boot" is a successful build. The `/health` URL in `specs/001-cost-sharing-app/quickstart.md` refers to an aspirational architecture that was never implemented.

## Interact
- **Primary**: dotnet CLI (build + test) + ADB (emulator interaction)
- **Endpoints / Commands**:
  - Build: `just build`
  - Test: `just test`
  - Test with evidence: `just test-log`
  - Full check (restore + build + test, self-healing): `just check`
  - Quick validate (build + test, self-healing): `just validate`
  - Full MAUI build: `just build-app` (Android target)
  - Clean: `just clean`
  - Environment diagnostics: `just doctor`
  - Setup emulator AVD (one-time, auto-installs image): `just setup-emulator`
  - Boot emulator (headless, auto-kills orphans): `just boot-emulator`
  - Build + deploy + launch app (auto-retries on crash): `just launch-app`
  - Capture screenshot: `just screenshot "label"`
  - ADB smoke test (3 screens): `just smoke-test`
  - Stop emulator: `just stop-emulator`
- **Auth Strategy**: Google OAuth (persistent profile via `appsettings.json` — client IDs, scopes)
- **Auth Expiry**: N/A for build/test cycle; OAuth tokens expire for runtime Google Drive features
- **Auth Detection**: Build/test cycle requires no auth; runtime auth failure surfaces as test assertion failure or exception

## Observe
- **Response capture**: stdout/stderr from `dotnet build` and `dotnet test`
- **Screenshots**: `just screenshot "label"` → `scratch/evidence/screenshot-{label}-{timestamp}.png`
- **Smoke test evidence**: `just smoke-test` → 3 screenshots per run in `scratch/evidence/`
- **Logs**: Build output (`-v detailed`), test results (`--logger "trx"`)
- **Evidence directory**: `./scratch/evidence/` (gitignored)
- **Self-healing signals**: `⚠️ Self-healed: <description>` messages in stdout when auto-recovery occurs
- **Exit codes**: 0 = clean success, 1 = unrecoverable failure, 2 = self-healed and succeeded

## Maturity Assessment
| Level | Status | Notes |
|-------|--------|-------|
| L0: No harness | ⬜ | Agent writes code, human tests |
| L1: Manual boot + API | ✅ | Human may need to set up .NET SDK; agent runs build + test |
| L2: Auto boot + API | ✅ | Agent runs `just validate`, named commands, evidence capture |
| L3: Full interaction + evidence | ✅ | Agent boots emulator, deploys app, drives UI via ADB, captures screenshots |
| L4: Self-healing | ✅ | Auto-recovery: stale builds, NuGet cache, orphan emulators, missing SDK, crash retry. `just doctor` for diagnostics |

Current: **L4** — All L3 capabilities plus self-healing. `just validate` and `just check` auto-recover from stale builds and NuGet corruption. `just boot-emulator` kills orphan emulators. `just setup-emulator` auto-installs missing system images. `just launch-app` retries on crash. `just doctor` diagnoses full environment. 16 named commands via justfile. Exit codes: 0=success, 1=unrecoverable, 2=self-healed.

## Validation Checklist
### Boot
- [x] Single command starts full stack (dotnet restore + build)
- [x] Health check endpoint/command exists and returns expected response (build succeeds)
- [x] Boot is idempotent (safe to run twice)
- [x] Handles port conflicts (N/A — no server)
- [x] Clean shutdown on SIGTERM/SIGINT (build process terminates cleanly)

### Interact
- [x] Agent can send input (dotnet CLI commands)
- [x] Agent can trigger all user-facing actions (emulator + ADB scripted UI navigation)
- [x] Auth is automated (no auth needed for build/test)
- [x] Auth expiry is detected with clear error message (N/A for build/test)

### Observe
- [x] Agent can read output (stdout/stderr from build + test)
- [x] Evidence capture works (`just test-log` → `scratch/evidence/test-results.trx`)
- [x] Structured output available (test results in trx format)

### Operate
- [x] Bootstrap doc explains harness to new agents (AGENTS.md + this file)
- [x] Example validation script exists (`just validate`)
- [x] Named commands exist (justfile with 15 commands)

## History
| Date | Plan | Change | Maturity Before → After |
|------|------|--------|------------------------|
| 2026-05-11 | 006-harness-infrastructure | Initial harness creation with justfile and AGENTS.md | — → L2 |
| 2026-05-11 | 001-harness-l3 | Added emulator lifecycle: setup, boot, launch, screenshot, smoke-test, stop | L2 → L3 |
| 2026-05-11 | 002-harness-l4 | Added self-healing: auto-recovery for builds, NuGet, emulator, SDK, crash retry. Added `just doctor`. Structured exit codes (0/1/2) | L3 → L4 |

<!-- USER CONTENT START -->
<!-- Project-specific harness notes, custom boot sequences, domain-specific setup -->

### Project Reality vs. Specs
The `specs/001-cost-sharing-app/quickstart.md` describes an aspirational architecture with a .NET backend API, React frontend, and Google Drive integration. The **actual implementation** is a standalone .NET MAUI mobile app with local SQLite storage only. The harness reflects the real codebase, not the spec.

### Test Suite
- 88 xUnit tests in `CostSharingApp/tests/CostSharingApp.Tests/`
- Coverage via `coverlet.collector`
- Run: `cd CostSharingApp && dotnet test tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj`

<!-- USER CONTENT END -->
