# ✈️ Flight Plan: Approved Fixtures for Financial Determinism

**Status**: Ready
**Spec**: approved-fixtures-spec.md
**Plan**: approved-fixtures-plan.md
**Complexity**: CS-2 (small)
**Branch**: `005-approved-fixtures`
**Mode**: Simple

## Mission

Add committed golden-master fixtures (JSON) for critical financial scenarios. Tests compare exact outputs against these fixtures. Behavior changes become visible as data diffs.

## Implementation (Simple — Single Phase)

| # | Task | Key Deliverable | Risk |
|---|------|----------------|------|
| T001 | Verify determinism | Confirm algorithms produce identical output across runs | Medium |
| T002 | Rounding fixtures | JSON golden file for split rounding edge cases | Low |
| T003 | Simplification fixtures | JSON golden file for debt routing scenarios | Low |
| T004 | Pipeline fixture | JSON golden file for end-to-end flow | Low |
| T005 | Test class | ApprovedFixtureTests.cs loading and asserting all fixtures | Low |
| T006 | Project config | .csproj includes JSON files in output | Low |
| T007 | Validation | `just validate` — all tests green | Low |

## Key Decisions

- **Fixture location**: Co-located in test project (`ApprovedFixtures/` directory)
- **Determinism approach**: Verify first, commit exact outputs
- **Format**: JSON golden files for clear diffs
- **Scope**: 3 fixture files (~6-10 scenarios total)

## Domain Impact

- **financial-core** [NEW sketch]: consume only — no service logic changes

## Success Criteria

All fixture tests pass alongside existing 91 tests. Rounding, simplification, and pipeline behavior locked down with exact committed outputs.

## Flight Log

| Date | Event |
|------|-------|
| 2026-05-14 | Spec created with research context |
| 2026-05-14 | Clarification complete (5/5 questions resolved) |
| 2026-05-14 | Plan generated — Ready for implementation |
