# L4 Self-Healing Harness Implementation Plan

**Mode**: Simple
**Plan Version**: 1.0.0
**Created**: 2026-05-11
**Spec**: `/Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/002-harness-l4/harness-l4-spec.md`
**Status**: COMPLETE

## Summary

The L3 harness provides emulator lifecycle recipes with fail-fast preflight checks, but agents still get stuck when the environment drifts (stale builds, NuGet corruption, orphan emulators, unregistered SDK images). This plan adds targeted self-healing wrappers around existing preflight checks, a new `just doctor` diagnostic recipe, and structured exit codes (0/1/2). All changes are in the justfile, harness.md, and AGENTS.md.

## Target Domains

| Domain | Status | Relationship | Role |
|--------|--------|-------------|------|
| _harness | existing | **modify** | justfile + harness.md + AGENTS.md |

## Domain Manifest

| File | Domain | Classification | Rationale |
|------|--------|---------------|-----------|
| `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | _harness | internal | All self-healing logic + doctor recipe |
| `/Users/azadehhassanzadeh/Source/cost-sharing/docs/project-rules/harness.md` | _harness | contract | L3→L4 maturity update |
| `/Users/azadehhassanzadeh/Source/cost-sharing/AGENTS.md` | _harness | contract | Add doctor command + self-healing docs |

## Key Findings

| # | Impact | Finding | Action |
|---|--------|---------|--------|
| F01 | High | `set -euo pipefail` will abort retry logic — need `set +e` scoping around recovery blocks | Wrap recovery probes with temporary `set +e`, restore after |
| F02 | High | `sdkmanager` is NOT on PATH — lives at `$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager` | Resolve from SDK tree, add `sdkmanager_bin` variable to justfile |
| F03 | High | Orphan emulator kill is risky — could terminate user's intentional session | Kill only by matching AVD name via `adb -s <serial> emu avd name`, not blind process kill |
| F04 | Medium | .NET build failures are classifiable — `NU1301`/`NU1101` = NuGet, `NETSDK*` = SDK, MSBuild = stale | Parse stderr for known recoverable patterns before retrying |
| F05 | Medium | Existing preflight checks in 5+ recipes are good hook points — layer recovery on top, don't duplicate | Reuse existing guard blocks, add `|| recover_and_retry` branches |
| F06 | Medium | `debug-android.sh` has manual recovery workflows (install, logcat, clear, restart) — anti-reinvention reference | Don't reinvent — borrow patterns for launch-app retry logic |

## Harness Strategy

- **Current Maturity**: L3
- **Target Maturity**: L4 (by end of this plan)
- **Boot Command**: `just check`
- **Health Check**: `just test`
- **Interaction Model**: Terminal (justfile recipes)
- **Evidence Capture**: Terminal output + screenshots
- **Pre-Phase Validation**: `just validate` must pass before implementation

## Implementation

**Objective**: Add self-healing recovery logic to all justfile recipes, create `just doctor`, update harness.md to L4.
**Testing Approach**: Lightweight — shell-level assertion checks via live recipe execution.

### Tasks

| Status | ID | Task | Domain | Path(s) | Done When | Notes |
|--------|-----|------|--------|---------|-----------|-------|
| [x] | T001 | Add `sdkmanager_bin` and `avdmanager_bin` variables to justfile header | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | Variables resolve to correct SDK paths | Per F02 — resolve from `android_home/cmdline-tools/latest/bin/` |
| [x] | T002 | Self-healing `validate` — detect stale build, auto-clean + rebuild | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just validate` succeeds after corrupting `bin/obj` | Try build → if fail, `dotnet clean` + retry → exit 2 on recovery, 1 on failure |
| [x] | T003 | Self-healing `check` — detect NuGet corruption, force-restore + rebuild | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just check` succeeds after deleting NuGet package | Try restore → if fail, `--force-evaluate` + retry → exit 2 on recovery |
| [x] | T004 | Self-healing `boot-emulator` — detect and kill orphan emulator, then boot | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just boot-emulator` succeeds with orphan emulator running | Check if running emulator matches our AVD → kill only ours → reboot. Per F03 |
| [x] | T005 | Self-healing `setup-emulator` — auto-install missing system image | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just setup-emulator` succeeds without pre-installed image | Use `sdkmanager_bin` to install, accept licenses with `--licenses` |
| [x] | T006 | Self-healing `launch-app` — detect crash, retry once with clean deploy | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just launch-app` retries on crash within 3s | Check process alive after 3s → if dead, `adb uninstall` + rebuild + retry |
| [x] | T007 | Create `just doctor` recipe — diagnose full environment | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just doctor` reports all 7 checks with ✅/❌ + fix commands | .NET SDK, Android SDK, emulator, AVD, ADB, system image, NuGet |
| [x] | T008 | Add `⚠️ Self-healed:` messages to all recovery paths | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | Every recovery action emits warning message | Per AC7 — agents must see what was auto-fixed |
| [x] | T009 | Update harness.md — L3→L4, document self-healing capabilities | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/docs/project-rules/harness.md` | L4 row checked, self-healing described | Update maturity table, Purpose, History |
| [x] | T010 | Update AGENTS.md — add `just doctor`, document exit codes | _harness | `/Users/azadehhassanzadeh/Source/cost-sharing/AGENTS.md` | Key Commands includes doctor, exit codes documented | |
| [x] | T011 | Verify all 88 tests still pass (`just validate`) | _harness | — | Exit 0, 88 tests pass | Regression check — self-healing must not break existing recipes |

### Acceptance Criteria

- [ ] AC1: `just validate` succeeds after corrupting `bin/`/`obj/` (auto-clean triggered)
- [ ] AC2: `just check` succeeds after deleting NuGet package (auto-restore triggered)
- [ ] AC3: `just boot-emulator` succeeds with orphan emulator running (auto-kill triggered)
- [ ] AC4: `just setup-emulator` succeeds with unregistered system image (auto-install triggered)
- [ ] AC5: `just launch-app` retries once if app crashes within 3s
- [ ] AC6: `just doctor` reports 7 checks with ✅/❌ + suggested fix commands
- [ ] AC7: All self-healing actions emit `⚠️ Self-healed: <description>` messages
- [ ] AC8: Exit code 2 on self-heal, 0 on clean success, 1 on unrecoverable

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Recovery masks real build errors | Medium | High | Only retry on specific known patterns (NuGet codes, stale obj) — not all failures |
| `sdkmanager` license acceptance blocks automation | Low | Medium | Use `yes \| sdkmanager --licenses` before install |
| Orphan kill terminates user's emulator | Low | High | Match by AVD name before killing (F03) |
| `set -euo pipefail` conflicts with retry logic | High | Medium | Scope `set +e` around recovery blocks only (F01) |
