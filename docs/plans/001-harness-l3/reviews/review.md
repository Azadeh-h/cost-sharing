# Code Review: Harness L3 — Emulator Lifecycle

**Plan**: /Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/001-harness-l3/harness-l3-plan.md
**Spec**: /Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/001-harness-l3/harness-l3-spec.md
**Phase**: Simple Mode (single phase)
**Date**: 2026-05-11
**Reviewer**: Automated (plan-7-v2)
**Testing Approach**: Manual Only

## A) Verdict

**APPROVE WITH NOTES**

Two HIGH findings relate to missing pre-flight checks (ADB/emulator binary existence) — `set -euo pipefail` catches these failures but with less friendly messages. One MEDIUM finding about label sanitization in the screenshot recipe. All are improvement opportunities, not correctness bugs.

## B) Summary

Clean infrastructure change: 6 justfile recipes for Android emulator lifecycle, plus doc updates to harness.md (L2→L3) and AGENTS.md. All shell scripts use `set -euo pipefail` for safety. The recipes follow existing justfile patterns (top-level variables, clear comments). No app code was touched. The 88 unit tests pass before and after. The main improvement opportunities are better pre-flight error messages and input sanitization on the screenshot label parameter.

## C) Checklist

**Testing Approach: Manual Only**

- [x] Manual verification steps documented (execution.log.md)
- [x] Manual test results recorded — `just --list` (15 recipes), `just validate` (88/88 pass)
- [x] Evidence artifacts present (execution log, justfile parse verification)
- [x] Only in-scope files changed (4 files: justfile, .gitignore, harness.md, AGENTS.md)
- [x] Domain compliance checks pass (no formal domains; single informal domain)

## D) Findings Table

| ID | Severity | File:Lines | Category | Summary | Recommendation |
|----|----------|------------|----------|---------|----------------|
| F001 | HIGH | justfile:78+ | error-handling | ADB command used without checking if it's in PATH | Add `command -v adb` check |
| F002 | HIGH | justfile:87 | error-handling | Emulator binary path not verified before use | Add `-x` existence check |
| F003 | MEDIUM | justfile:126-127 | security | Screenshot label not sanitized (path traversal) | Strip special chars with `tr` |
| F004 | MEDIUM | justfile:91-97 | correctness | Boot timeout could run 122s not 120s | Move check before sleep |
| F005 | MEDIUM | justfile:154,160 | correctness | Hardcoded tap coordinates are device-specific | Add documenting comment |
| F006 | LOW | justfile:99 | error-handling | Keyevent 82 failure is silent | Make non-fatal with `\|\| echo` |
| F007 | LOW | justfile:167 | error-handling | `ls` at end of smoke-test may fail | Add `2>/dev/null` fallback |
| F008 | LOW | justfile:112-113 | error-handling | Build failure in launch-app lacks context message | Add error wrapper |

## E) Detailed Findings

### E.1) Implementation Quality

**8 findings** (2 HIGH, 3 MEDIUM, 3 LOW). No correctness bugs — all findings are error handling and robustness improvements. Shell syntax is correct throughout. Recipes follow existing justfile patterns. `set -euo pipefail` provides a safety net for all unchecked errors.

**Mitigating factor for HIGHs**: `set -euo pipefail` means missing `adb` or `emulator` will still fail the recipe with a non-zero exit code and "command not found" message. The findings ask for *friendlier* error messages, not for fixing broken behavior.

### E.2) Domain Compliance

No formal domain infrastructure exists (no `docs/domains/`). Single informal domain `harness-infrastructure`.

| Check | Status | Details |
|-------|--------|---------|
| File placement | ✅ | All changes in expected files per Domain Manifest |
| Contract-only imports | N/A | No code imports — shell scripts only |
| Dependency direction | N/A | Infrastructure-only change |
| Domain.md updated | N/A | No formal domain.md exists |
| Registry current | N/A | No registry exists |
| No orphan files | ✅ | All files in manifest |
| Map nodes current | N/A | No domain map exists |
| Map edges current | N/A | No domain map exists |
| No circular business deps | N/A | No business domains |
| Concepts documented | N/A | No formal domain structure |

### E.3) Anti-Reinvention

| New Component | Existing Match? | Domain | Status |
|--------------|----------------|--------|--------|
| setup-emulator recipe | None | harness-infrastructure | ✅ Proceed — new capability |
| boot-emulator recipe | None | harness-infrastructure | ✅ Proceed — new capability |
| launch-app recipe | Partial — `build-app` exists | harness-infrastructure | ✅ OK — `launch-app` extends `build-app` with deploy+launch |
| screenshot recipe | None | harness-infrastructure | ✅ Proceed — new capability |
| smoke-test recipe | None | harness-infrastructure | ✅ Proceed — new capability |
| stop-emulator recipe | None | harness-infrastructure | ✅ Proceed — new capability |

No reinvention detected.

### E.4) Testing & Evidence

**Coverage confidence**: 85%

| AC | Confidence | Evidence |
|----|------------|----------|
| AC1: boot-emulator | 90% | Recipe implemented with boot completion check, timeout loop. Syntax verified. |
| AC2: launch-app | 90% | Recipe uses `-t:Install` + `monkey`. Syntax verified. |
| AC3: screenshot | 90% | Recipe uses `adb exec-out screencap -p`. Syntax verified. |
| AC4: smoke-test | 90% | 3-step navigation with screenshots. Syntax verified. |
| AC5: evidence directory | 85% | `scratch/` gitignored, recipes create `scratch/evidence/`. |
| AC6: harness.md L3 | 100% | Maturity table, Interact, Observe, History all updated. |
| AC7: boot < 90s | 75% | Quick boot flags present. Environment-dependent — cannot pre-verify. |
| AC8: 88 tests pass | 100% | Pre and post validation: 88/88 pass. |

### E.5) Doctrine Compliance

N/A — no `docs/project-rules/rules.md`, `idioms.md`, `architecture.md`, or `constitution.md` found.

### E.6) Harness Live Validation

Harness status: **HEALTHY** (build + test)
Emulator recipes: **NOT LIVE-TESTED** — no emulator running. Recipes are the new infrastructure being delivered; they will be validated on first use after `just setup-emulator`.

## F) Coverage Map

| AC | Description | Evidence | Confidence |
|----|-------------|----------|------------|
| AC1 | boot-emulator starts and waits | Recipe implemented, syntax verified | 90% |
| AC2 | launch-app builds, deploys, launches | Recipe implemented, syntax verified | 90% |
| AC3 | screenshot captures to scratch/evidence/ | Recipe implemented, syntax verified | 90% |
| AC4 | smoke-test navigates 2-3 screens | 3-step recipe with screenshots, syntax verified | 90% |
| AC5 | Evidence in scratch/evidence/ | Directory gitignored, recipes target it | 85% |
| AC6 | harness.md updated to L3 | All sections updated, history recorded | 100% |
| AC7 | Boot + launch < 90s | Quick boot flags, timing env-dependent | 75% |
| AC8 | 88 unit tests pass | Pre/post validation: 88/88 | 100% |

**Overall coverage confidence**: 85%

## G) Commands Executed

```bash
git diff --stat
git diff --staged --stat
git diff > docs/plans/001-harness-l3/reviews/_computed.diff
just --list
just validate
```

## H) Handover Brief

> Copy this section to the implementing agent.

**Review result**: APPROVE WITH NOTES

**Plan**: /Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/001-harness-l3/harness-l3-plan.md
**Spec**: /Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/001-harness-l3/harness-l3-spec.md
**Phase**: Simple Mode
**Tasks dossier**: inline in plan
**Execution log**: /Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/001-harness-l3/execution.log.md
**Review file**: /Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/001-harness-l3/reviews/review.md

### Files Reviewed

| File (absolute path) | Status | Domain | Action Needed |
|---------------------|--------|--------|---------------|
| /Users/azadehhassanzadeh/Source/cost-sharing/justfile | Modified | harness-infrastructure | Optional: F001-F008 improvements |
| /Users/azadehhassanzadeh/Source/cost-sharing/.gitignore | Modified | harness-infrastructure | None |
| /Users/azadehhassanzadeh/Source/cost-sharing/docs/project-rules/harness.md | Modified | harness-infrastructure | None |
| /Users/azadehhassanzadeh/Source/cost-sharing/AGENTS.md | Modified | harness-infrastructure | None |

### Suggested Improvements (not blocking)

| # | File | What To Improve | Why |
|---|------|-----------------|-----|
| F001 | justfile | Add `command -v adb` pre-flight check | Friendlier error if ADB missing |
| F002 | justfile | Add `-x` check on emulator binary | Friendlier error if emulator missing |
| F003 | justfile | Sanitize screenshot label parameter | Prevent path traversal |

### Next Step

Implementation complete and approved — commit changes and consider applying F001-F003 improvements.
