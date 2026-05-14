# ✈️ Flight Plan: Add Mutation Testing

**Plan**: 005-mutation-testing
**Status**: Landed
**Mode**: Simple
**Complexity**: CS-2 (small) — P=4, Confidence: 0.85
**Generated**: 2026-05-13

## Mission

Add Stryker.NET mutation testing to the cost-sharing app, targeting `CostSharing.Core` (financial calculation logic). Produce a mutation score baseline and integrate into the developer workflow via `just mutate`.

## Architecture Vision

Single-phase implementation: install Stryker.NET as a local dotnet tool, configure it to mutate only `CostSharing.Core`, add `just mutate` recipe, document in AGENTS.md and harness.md. No production code changes. No new tests. Pure tooling/config.

## Spec Summary

| Attribute | Value |
|-----------|-------|
| Goals | Measure test effectiveness, reveal weak assertions, protect financial correctness |
| Non-Goals | Mutating MAUI app, 100% score, adding new tests, CI/CD integration |
| Target Domain | `testing-infrastructure` (NEW) |
| Mutation Scope | `SplitCalculationService`, `DebtCalculationService`, `DebtSimplificationAlgorithm` |
| Acceptance Criteria | 10 testable scenarios |

## Key Decisions (from Clarify)

| Decision | Choice |
|----------|--------|
| Mode | Simple (single-phase, CS-2) |
| Testing | Lightweight — verify Stryker runs and produces report |
| Documentation | AGENTS.md + harness.md only |
| Reports | Gitignored (local-only) |
| Break threshold | 60% (lenient start) |
| Harness | Sufficient at L4, add `just mutate` |

## Key Research Findings

- `CostSharing.Core` is a pure `net9.0` library — perfect Stryker target (no MAUI deps)
- 75 tests across 6 classes; several weak assertions will let mutants survive
- No dotnet tool manifest exists — must create with `dotnet new tool-manifest`
- No `.gitignore` entry for `StrykerOutput/` — must add
- Expected initial mutation score: 60-75%

## Tasks (8 total)

| ID | Task | Done When |
|----|------|-----------|
| T001 ✅ | Create tool manifest + install Stryker | `dotnet tool list` shows `dotnet-stryker` |
| T002 ✅ | Create stryker-config.json | Config has correct paths, thresholds 60/70/80 |
| T003 ✅ | Run initial Stryker baseline | Score: 64.34% (Split: 90.91%, Algo: 61.76%, Debt: 53.95%) |
| T004 ✅ | Add `just mutate` recipe | `just mutate` runs Stryker end-to-end |
| T005 ✅ | Add StrykerOutput/ to .gitignore | Entry present |
| T006 ✅ | Update AGENTS.md | Key Commands + Mutation Testing section added |
| T007 ✅ | Update harness.md | Interact section + History updated |
| T008 ✅ | Verify `just validate` passes | 91 tests pass, no regressions |

## Risks

| Risk | Mitigation |
|------|------------|
| Stryker.NET .NET 9 compatibility | Check latest version; fallback to net8.0 target |
| Low initial score (~60%) | Expected — document as baseline, not failure |
| Slow mutation runs | Scope to Tier 1 only; concurrent test runners |

## Next Steps

- [ ] Run `/plan-7-v2-code-review --plan "docs/plans/005-mutation-testing/mutation-testing-plan.md"` for review

## Flight Log

| Date | Event |
|------|-------|
| 2026-05-13 | Spec created (plan-1b) |
| 2026-05-13 | 7 clarifications resolved (plan-2) |
| 2026-05-13 | Plan generated — Simple mode, 8 tasks (plan-3) |
| 2026-05-13 | Implementation complete — all 8 tasks done, 10/10 ACs met, score 64.34% (plan-6) |
