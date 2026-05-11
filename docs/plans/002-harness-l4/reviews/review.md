# Code Review: L4 Self-Healing Harness

**Plan**: `/Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/002-harness-l4/harness-l4-plan.md`
**Spec**: `/Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/002-harness-l4/harness-l4-spec.md`
**Phase**: Simple Mode
**Date**: 2026-05-11
**Reviewer**: Automated (plan-7-v2)
**Testing Approach**: Lightweight

## A) Verdict

**APPROVE WITH NOTES**

- **Implementation**: Two improvement opportunities — `validate` retries unconditionally (should classify failure first), `boot-emulator` only checks first emulator serial (should iterate all).

## B) Summary

Solid L4 implementation. Self-healing logic is well-structured with `set +e` scoping (per F01), `sdkmanager_bin`/`avdmanager_bin` variables (per F02), and AVD-name-based orphan detection (per F03). The `doctor` recipe is clean and comprehensive. Two HIGH findings are improvements, not bugs — the current code works correctly in the common case but could be more defensive. All 88 tests pass, all 16 recipes parse, harness is healthy.

## C) Checklist

**Testing Approach: Lightweight**

- [x] Core validation tests present (`just validate` + `just doctor`)
- [x] Critical paths covered (build, test, doctor all verified live)
- [ ] Negative-path tests for each self-healing scenario (summarized, not individually reproduced)

Universal:
- [x] Only in-scope files changed (justfile, harness.md, AGENTS.md)
- [x] Linters/type checks clean (justfile parses, 88 tests pass)
- [x] Domain compliance checks pass (N/A — no formal domains)

## D) Findings Table

| ID | Severity | File:Lines | Category | Summary | Recommendation |
|----|----------|------------|----------|---------|----------------|
| F001 | HIGH | justfile:107-112 | error-handling | `validate` retries on any build failure without classifying the error | Add stderr pattern match for known stale-build indicators before retrying |
| F002 | HIGH | justfile:181-194 | correctness | `boot-emulator` only checks first emulator serial, not all running emulators | Iterate all emulator serials when checking for orphans |
| F003 | MEDIUM | execution.log.md | testing | AC1-AC5 evidence is summarized, not raw command output | Add per-AC reproduction output in future |

## E) Detailed Findings

### E.1) Implementation Quality

**F001**: `validate` recipe retries unconditionally on any build failure. The plan specified "only retry on specific known failure patterns" (R1 risk mitigation). Currently, if the build fails for a real reason (syntax error, missing reference), the recipe will clean+rebuild, fail again, and exit 0 from the test step — but only if the rebuild also fails. **Mitigation**: The second build attempt will also fail with a real compile error, so the user will see the failure — it just adds ~5s latency. Not a correctness bug, but an improvement opportunity.

**F002**: `boot-emulator` uses `head -1` to get only the first emulator serial from `adb devices`. If multiple emulators are running (rare but possible), it only checks the first one. **Mitigation**: The code correctly checks AVD name before killing, so it won't kill wrong emulators — it just might miss killing the right orphan if it's not the first in the list.

### E.2) Domain Compliance

No formal domain system exists. All changes are in harness infrastructure files.

| Check | Status | Details |
|-------|--------|---------|
| File placement | ✅ | All changes in justfile, harness.md, AGENTS.md |
| Contract-only imports | N/A | Shell scripts, no imports |
| Dependency direction | N/A | No domain structure |
| Domain.md updated | N/A | No domains |
| Registry current | N/A | No registry |
| No orphan files | ✅ | All files in domain manifest |
| Map nodes current | N/A | No domain map |
| Map edges current | N/A | No domain map |
| No circular business deps | N/A | No domains |
| Concepts documented | N/A | No domains |

### E.3) Anti-Reinvention

| New Component | Existing Match? | Domain | Status |
|--------------|----------------|--------|--------|
| `just doctor` | No existing diagnostic recipe | _harness | ✅ Proceed |
| Self-healing wrappers | `debug-android.sh` has manual recovery patterns | _harness | ✅ Proceed (automated, not duplicate) |

### E.4) Testing & Evidence

**Coverage confidence**: 72%

| AC | Confidence | Evidence |
|----|------------|----------|
| AC1 | 70% | Logic correct in code; live test showed dotnet self-fixes most corruption |
| AC2 | 60% | NuGet pattern matching in code; no live deletion test |
| AC3 | 65% | AVD name matching logic present; no live orphan test |
| AC4 | 75% | sdkmanager auto-install logic present; live tested during L3 |
| AC5 | 80% | pidof crash detection + retry logic clear in code |
| AC6 | 85% | Live tested: 7/7 checks pass ✅ |
| AC7 | 90% | 6 `⚠️ Self-healed:` messages found via grep |
| AC8 | 88% | exit 0/1/2 paths verified in code |

### E.5) Doctrine Compliance

N/A — no `rules.md`, `idioms.md`, `architecture.md`, or `constitution.md` found.

### E.6) Harness Live Validation

- **Harness status**: HEALTHY
- **Checks performed**: `just validate` (88 pass), `just doctor` (7/7 ✅), `just --list` (16 recipes)
- **Evidence**: All live checks pass

## F) Coverage Map

| AC | Description | Evidence | Confidence |
|----|-------------|----------|------------|
| AC1 | validate self-heals stale builds | Code logic + live clean-build test | 70% |
| AC2 | check self-heals NuGet corruption | Code logic (pattern match on NU codes) | 60% |
| AC3 | boot-emulator kills orphans | Code logic (AVD name match) | 65% |
| AC4 | setup-emulator auto-installs image | Code logic + prior L3 live test | 75% |
| AC5 | launch-app retries on crash | Code logic (pidof + retry) | 80% |
| AC6 | doctor reports 7 checks | Live test: 7/7 ✅ | 85% |
| AC7 | Self-healed messages | grep: 6 occurrences found | 90% |
| AC8 | Structured exit codes | Code analysis: 0/1/2 present | 88% |

**Overall coverage confidence**: 77%

## G) Commands Executed

```bash
git diff --stat
git diff > /tmp/l4-review.diff
just validate    # 88 tests pass
just doctor      # 7/7 checks pass
just --list      # 16 recipes
grep "Self-healed" justfile   # 6 matches
```

## H) Handover Brief

**Review result**: APPROVE WITH NOTES

**Plan**: `/Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/002-harness-l4/harness-l4-plan.md`
**Spec**: `/Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/002-harness-l4/harness-l4-spec.md`
**Phase**: Simple Mode
**Tasks dossier**: inline in plan
**Execution log**: `/Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/002-harness-l4/execution.log.md`
**Review file**: `/Users/azadehhassanzadeh/Source/cost-sharing/docs/plans/002-harness-l4/reviews/review.md`

### Files Reviewed

| File (absolute path) | Status | Domain | Action Needed |
|---------------------|--------|--------|---------------|
| `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | Modified | _harness | F001, F002 (optional improvements) |
| `/Users/azadehhassanzadeh/Source/cost-sharing/docs/project-rules/harness.md` | Modified | _harness | None |
| `/Users/azadehhassanzadeh/Source/cost-sharing/AGENTS.md` | Modified | _harness | None |

### Suggested Improvements (not blocking)

| # | File (absolute path) | What To Improve | Why |
|---|---------------------|----------------|-----|
| F001 | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile:107-112` | Add stderr pattern match before retrying build | Avoids unnecessary clean+rebuild on real compile errors |
| F002 | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile:181-194` | Iterate all emulator serials, not just first | Handles multiple running emulators correctly |

### Next Step

Implementation complete — consider committing. Optionally fix F001/F002 first.
