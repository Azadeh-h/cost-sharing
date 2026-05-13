# ✈️ Flight Plan: Continuous Drift Detection

**Plan**: 006-drift-detection
**Status**: Landed
**Mode**: Simple
**Complexity**: CS-3 (medium) — P=6, Confidence: 0.75
**Generated**: 2026-05-13

## Mission

Add continuous drift detection sensors — dead code, coverage quality, dependency scanning, and doc drift — to the cost-sharing app. Produce a unified `just drift` command that reports codebase health in an agent-consumable format.

## Architecture Vision

Single script (`scripts/drift-scan.sh`) with 4 sensor functions, wired into justfile as `just drift`. Each sensor outputs a status section; the script combines them into a unified JSON report streamed to stdout. No production code changes.

## Spec Summary

| Attribute | Value |
|-----------|-------|
| Goals | Detect dead code, measure coverage quality, scan dependencies, catch doc drift |
| Non-Goals | Auto-fixing drift, CI/CD integration, runtime monitoring |
| Target Domain | `testing-infrastructure` (modify) |
| Sensors | 4: dependency scanner, coverage quality, dead code, doc drift |
| Acceptance Criteria | 10 testable scenarios |

## Key Decisions (from Clarify)

| Decision | Choice |
|----------|--------|
| Mode | Simple (single-phase, CS-3) |
| Testing | Lightweight — verify sensors run and produce output |
| Documentation | AGENTS.md + harness.md only |
| Dead code scope | Interfaces only |
| Output format | Stdout stream (JSON) |
| Harness | Sufficient at L4, add `just drift` |

## Key Research Findings

- 6 interfaces to scan; 4 have no test coverage (Drive, Auth, Sync, OfflineQueue, ConflictResolver)
- DI-registered interfaces will false-positive — need exclusion list
- `coverlet.collector` sufficient for CLI coverage collection
- AGENTS.md has stale test count (88 vs 91) — doc drift sensor will catch this
- Dependency scan requires network; handle offline gracefully

## Tasks (9 total)

| ID | Task | Done When |
|----|------|-----------|
| ~~T001~~ | ~~Dependency scanner sensor~~ | ✅ 13 outdated, 0 vulnerable |
| ~~T002~~ | ~~Coverage quality sensor~~ | ✅ 79% overall, 17 files |
| ~~T003~~ | ~~Dead code sensor (interfaces)~~ | ✅ 0 dead, 6 untested DI |
| ~~T004~~ | ~~Doc drift sensor~~ | ✅ 3 drifts detected |
| ~~T005~~ | ~~Unified report + health summary~~ | ✅ JSON with verdict |
| ~~T006~~ | ~~Add `just drift` recipe~~ | ✅ Wired in justfile |
| ~~T007~~ | ~~Update AGENTS.md~~ | ✅ Key Commands + section |
| ~~T008~~ | ~~Update harness.md~~ | ✅ Interact + History |
| ~~T009~~ | ~~Verify `just validate` passes~~ | ✅ 91 tests, exit 0 |

## Risks

| Risk | Mitigation |
|------|------------|
| Dead code false positives (DI) | Exclusion list for DI-registered interfaces |
| Dependency scan offline | Graceful degradation — "skipped" |
| Doc drift regex fragility | Target specific numeric facts only |

## Next Steps

- [x] Implementation complete — all 9 tasks done, all 10 ACs met
- [ ] Optional: Run `/plan-7-v2-code-review --plan "docs/plans/006-drift-detection/drift-detection-plan.md"`

## Flight Log

| Date | Event |
|------|-------|
| 2026-05-13 | Spec created (plan-1b) |
| 2026-05-13 | 7 clarifications resolved (plan-2) |
| 2026-05-13 | Plan generated — Simple mode, 9 tasks (plan-3) |
| 2026-05-13 | All 9 tasks implemented — 4 sensors operational, `just drift` live (plan-6) |
