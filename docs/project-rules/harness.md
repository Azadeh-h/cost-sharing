# Agent Harness

**Version**: 1.0.0
**Created**: 2026-05-11
**Maturity Level**: L3
**Project Type**: mobile (.NET MAUI — Android only)

## Purpose
Enable agents to iterate on the CostSharingApp — a .NET MAUI mobile app with local SQLite storage — using a build → test → observe cycle in 30–60 second loops. At L3, agents can also boot an Android emulator, deploy and launch the app, drive UI interactions via ADB, and capture screenshot evidence.

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
  - Full check (restore + build + test): `just check`
  - Full MAUI build: `just build-app` (Android target)
  - Clean: `just clean`
  - Setup emulator AVD (one-time): `just setup-emulator`
  - Boot emulator (headless): `just boot-emulator`
  - Build + deploy + launch app: `just launch-app`
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

## Maturity Assessment
| Level | Status | Notes |
|-------|--------|-------|
| L0: No harness | ⬜ | Agent writes code, human tests |
| L1: Manual boot + API | ✅ | Human may need to set up .NET SDK; agent runs build + test |
| L2: Auto boot + API | ✅ | Agent runs `just validate`, named commands, evidence capture |
| L3: Full interaction + evidence | ✅ | Agent boots emulator (`just boot-emulator`), deploys app (`just launch-app`), drives UI via ADB, captures screenshots |
| L4: Self-healing | ⬜ | Auto-recovery from stale builds, NuGet cache issues |

Current: **L3** — Agent runs `just validate` (build + 88 tests), boots emulator (`just boot-emulator`), deploys and launches app (`just launch-app`), captures screenshots (`just screenshot`), runs ADB-scripted smoke tests (`just smoke-test`). 15 named commands via justfile.

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

<!-- USER CONTENT START -->
<!-- Project-specific harness notes, custom boot sequences, domain-specific setup -->

### Project Reality vs. Specs
The `specs/001-cost-sharing-app/quickstart.md` describes an aspirational architecture with a .NET backend API, React frontend, and Google Drive integration. The **actual implementation** is a standalone .NET MAUI mobile app with local SQLite storage only. The harness reflects the real codebase, not the spec.

### Test Suite
- 88 xUnit tests in `CostSharingApp/tests/CostSharingApp.Tests/`
- Coverage via `coverlet.collector`
- Run: `cd CostSharingApp && dotnet test tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj`

<!-- USER CONTENT END -->
