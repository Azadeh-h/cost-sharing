# Harness L3: Emulator Boot + UI Testing — Implementation Plan

**Mode**: Simple
**Plan Version**: 1.0.0
**Created**: 2026-05-11
**Spec**: [harness-l3-spec.md](harness-l3-spec.md)
**Workshop**: [workshops/001-emulator-lifecycle.md](workshops/001-emulator-lifecycle.md)
**Status**: COMPLETE

## Summary

Upgrade the agent harness from L2 (build + test) to L3 (full interaction + evidence) by adding Android emulator lifecycle recipes to the justfile. Agents will be able to boot an emulator, deploy and launch the app, capture screenshots, run ADB-scripted smoke tests, and shut down — all via named `just` commands. This closes the feedback loop: agents can visually verify their changes work in the running app.

## Target Domains

| Domain | Status | Relationship | Role |
|--------|--------|-------------|------|
| harness-infrastructure | existing (informal) | **modify** | Add emulator boot, app launch, screenshot capture, smoke test recipes |

## Domain Manifest

| File | Domain | Classification | Rationale |
|------|--------|---------------|-----------|
| `justfile` | harness-infrastructure | internal | Add 6 new recipes: setup-emulator, boot-emulator, launch-app, screenshot, smoke-test, stop-emulator |
| `.gitignore` | harness-infrastructure | internal | Add `scratch/` to prevent evidence artifacts from being committed |
| `docs/project-rules/harness.md` | harness-infrastructure | contract | Upgrade maturity L2→L3, document new capabilities |
| `AGENTS.md` | harness-infrastructure | contract | Add emulator commands to Key Commands table |

## Key Findings

| # | Impact | Finding | Action |
|---|--------|---------|--------|
| F01 | Critical | System image is `arm64-v8a` (Apple Silicon), NOT `x86_64` as workshop assumed | Use `system-images;android-35;google_apis;arm64-v8a` in AVD creation |
| F02 | High | `scratch/` is NOT in `.gitignore` — screenshots would be committed | Add `scratch/` to `.gitignore` as first task |
| F03 | High | `timeout` command doesn't exist on macOS | Use shell loop with `sleep` + counter instead of `timeout` |
| F04 | Medium | AndroidManifest.xml has no package/activity declaration — derived from .csproj | Use `monkey -p com.costsharingapp.mobile` for launch (avoids `crc64` activity hash) |
| F05 | Low | Justfile style uses 4 top-level vars + simple inline recipes | Follow same pattern: add vars for `avd_name`, `android_home`, `emulator`, `app_package` |

## Harness Strategy

- **Current Maturity**: L2
- **Target Maturity**: L3 (by end of implementation)
- **Boot Command**: `just check`
- **Health Check**: `just test`
- **Interaction Model**: ADB shell commands (tap, swipe, screencap)
- **Evidence Capture**: Screenshots → `scratch/evidence/`, test logs → `scratch/evidence/`
- **Pre-Phase Validation**: `just validate` (88 tests must pass before and after)

## Implementation

**Objective**: Add 6 emulator lifecycle recipes to justfile, update harness docs to L3.
**Testing Approach**: Manual Only — verify each recipe works by running it.

### Tasks

| Status | ID | Task | Domain | Path(s) | Done When | Notes |
|--------|-----|------|--------|---------|-----------|-------|
| [x] | T001 | Add `scratch/` to `.gitignore` | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/.gitignore` | `grep scratch .gitignore` returns match | Per F02 |
| [x] | T002 | Add emulator variables to justfile | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | Variables `avd_name`, `android_home`, `emulator`, `app_package` defined at top | Per F05; use `arm64-v8a` per F01 |
| [x] | T003 | Add `setup-emulator` recipe | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just setup-emulator` creates AVD `cost-sharing-test` if not exists, errors if system image missing | Per workshop; use `arm64-v8a` per F01 |
| [x] | T004 | Add `boot-emulator` recipe | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just boot-emulator` starts headless emulator, waits for `sys.boot_completed==1`, unlocks screen | Per workshop; shell loop per F03 |
| [x] | T005 | Add `launch-app` recipe | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just launch-app` builds with `-t:Install`, launches via `monkey`, waits for render | Per workshop + F04 |
| [x] | T006 | Add `screenshot` recipe | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just screenshot "label"` saves PNG to `scratch/evidence/screenshot-{label}-{timestamp}.png` | Per workshop |
| [x] | T007 | Add `smoke-test` recipe | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just smoke-test` launches app, navigates 2-3 screens via ADB tap, captures screenshot at each step | ADB-scripted |
| [x] | T008 | Add `stop-emulator` recipe | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just stop-emulator` gracefully kills emulator via `adb emu kill`, saves snapshot | Per workshop |
| [x] | T009 | Update `harness.md` to L3 | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/docs/project-rules/harness.md` | Maturity table shows L3 ✅, Interact section lists emulator commands, Observe section lists screenshot capture | |
| [x] | T010 | Update `AGENTS.md` with emulator commands | harness-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/AGENTS.md` | Key Commands table includes setup-emulator, boot-emulator, launch-app, screenshot, smoke-test, stop-emulator | |
| [x] | T011 | Validate existing tests still pass | harness-infrastructure | — | `just validate` → 88/88 pass | Regression gate |

### Acceptance Criteria

- [x] `just boot-emulator` starts an Android emulator and waits until it's ready (boot complete)
- [x] `just launch-app` builds, deploys, and launches the app on the running emulator
- [x] `just screenshot` captures the current emulator screen and saves to `scratch/evidence/`
- [x] `just smoke-test` runs ADB-scripted navigation through 2-3 main screens with screenshot capture
- [x] Smoke tests produce screenshot evidence in `scratch/evidence/` for each navigation step
- [x] `docs/project-rules/harness.md` is updated to L3 maturity
- [x] Emulator boot + app launch completes in under 90 seconds from cold start
- [x] All existing tests (88 unit tests) continue to pass

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Emulator boot too slow for feedback loops | Medium | Medium | Quick boot from snapshot (~10-15s); cold boot only on first run |
| ADB tap coordinates wrong for smoke-test | Medium | Low | Capture screenshot first, determine coordinates empirically; keep smoke test simple |
| `scratch/` already has untracked files | Low | Low | Check before adding to `.gitignore`; won't affect tracked files |
| System image `arm64-v8a` not compatible with `pixel_7` device profile | Low | Medium | Fall back to generic device if needed |
