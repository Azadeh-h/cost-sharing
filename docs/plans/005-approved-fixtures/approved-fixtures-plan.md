# Approved Fixtures for Financial Determinism — Implementation Plan

**Mode**: Simple
**Plan Version**: 1.0.0
**Created**: 2026-05-14
**Spec**: approved-fixtures-spec.md
**Status**: COMPLETE

## Summary

Add committed golden-master fixture tests for the Cost Sharing App's critical financial scenarios. JSON fixture files define exact expected outputs for rounding, debt simplification, and end-to-end pipeline scenarios. New test classes compare actual service outputs against these fixtures exactly. This is purely additive — no existing tests or service logic are modified.

## Target Domains

| Domain | Status | Relationship | Role |
|--------|--------|-------------|------|
| financial-core | **NEW** (sketch) | **consume** | All fixture scenarios exercise split, debt, and simplification services — no service changes |

## Domain Manifest

| File | Domain | Classification | Rationale |
|------|--------|---------------|-----------|
| `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/rounding-scenarios.json` | financial-core | internal | Golden outputs for split rounding edge cases |
| `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/simplification-scenarios.json` | financial-core | internal | Golden outputs for debt simplification routing |
| `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/pipeline-scenarios.json` | financial-core | internal | Golden outputs for end-to-end expense→settlement flow |
| `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/ApprovedFixtureTests.cs` | financial-core | internal | Test class loading and asserting against all fixture files |
| `CostSharingApp/tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj` | financial-core | internal | Add EmbeddedResource or CopyToOutput for JSON fixtures |

## Key Findings

| # | Impact | Finding | Action |
|---|--------|---------|--------|
| 01 | High | The GreedyMatching algorithm uses `OrderByDescending(b => b.Value)` — deterministic when balances are distinct, but tie-breaking on equal balances depends on Dictionary enumeration order | T001 verifies determinism by running each scenario 10x and checking for identical output |
| 02 | High | Existing tests use `Guid.NewGuid()` which produces random IDs per run — fixture tests must use FIXED GUIDs for reproducibility | Use deterministic GUIDs in fixture test setup (e.g., `new Guid("00000000-0000-0000-0000-000000000001")`) |
| 03 | Medium | The `.csproj` needs to include JSON files as content that gets copied to output directory, otherwise tests can't find them at runtime | Add `<Content Include="ApprovedFixtures\**\*.json" CopyToOutputDirectory="PreserveNewest" />` to csproj |
| 04 | Medium | `SplitCalculationService.CalculateEvenSplit` assigns remainder to first participant (index 0) — this is the deterministic policy to lock down | Fixture asserts first participant gets the extra cent |

## Harness Strategy

- **Current Maturity**: L4 (self-healing)
- **Target Maturity**: L4 (no change needed)
- **Boot Command**: `just check`
- **Health Check**: `just test`
- **Interaction Model**: Terminal (dotnet test)
- **Evidence Capture**: Test results via `just validate`
- **Pre-Phase Validation**: Run `just validate` before and after implementation

## Implementation

**Objective**: Add JSON-based approved fixture tests for rounding, simplification, and pipeline scenarios
**Testing Approach**: Lightweight — `just validate` confirms all fixture tests pass alongside existing 91 tests

### Tasks

| Status | ID | Task | Domain | Path(s) | Done When | Notes |
|--------|-----|------|--------|---------|-----------|-------|
| [x] | T001 | Verify algorithm determinism — run SplitCalculation, DebtSimplification on test inputs 10x each, confirm identical output every time | financial-core | (exploratory — no file changes) | All 10 runs produce identical outputs for each scenario | Per finding 01; if non-deterministic, STOP and report |
| [x] | T002 | Create `ApprovedFixtures/` directory and add JSON fixture files for rounding scenarios | financial-core | `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/rounding-scenarios.json` | JSON file contains exact expected outputs for: $100/3, $10/3, $1/3, $99.99/7, $100.01 50/50, decimal percentages 33.33/33.33/33.34 of $100 | Per finding 04 — first participant gets remainder |
| [x] | T003 | Create JSON fixture file for simplification scenarios | financial-core | `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/simplification-scenarios.json` | JSON contains exact transaction routing for: circular 3-person, complex 4-person, symmetric 2-creditor, large amounts | Per finding 01 — verified deterministic in T001 |
| [x] | T004 | Create JSON fixture file for end-to-end pipeline scenario | financial-core | `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/pipeline-scenarios.json` | JSON contains full flow: expense inputs → split outputs → debt outputs → simplified debts → post-settlement remainder | Scenario 5 from research dossier |
| [x] | T005 | Create ApprovedFixtureTests.cs test class | financial-core | `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/ApprovedFixtureTests.cs` | Test class loads each JSON fixture, runs services with fixture inputs, asserts outputs match exactly | Uses fixed GUIDs per finding 02; uses System.Text.Json for deserialization |
| [x] | T006 | Update .csproj to include JSON fixtures in test output | financial-core | `CostSharingApp/tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj` | `<Content Include="ApprovedFixtures/**/*.json" CopyToOutputDirectory="PreserveNewest" />` added | Per finding 03 |
| [x] | T007 | Run `just validate` — all tests pass (91 existing + new fixture tests) | financial-core | — | Exit code 0, all tests green | Final verification |

### Acceptance Criteria

- [x] AC1: `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/` directory exists with JSON golden files
- [x] AC2: Rounding tests assert exact per-person amounts (who gets the extra cent)
- [x] AC3: Simplification tests assert exact transaction routing (from → to → amount)
- [x] AC4: At least one end-to-end fixture covers expense → split → debt → simplification → post-settlement
- [x] AC5: All fixture tests pass with current service implementations (no logic changes)
- [x] AC6: JSON files are human-readable and produce clear diffs
- [x] AC7: Rounding gauntlet ($1/3, $10/3, $100/3, $99.99/7) has committed exact outputs

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Equal-balance tie-breaking in simplification is non-deterministic | Low | High | T001 verifies determinism first; if found, add stable sort by Guid before committing fixtures |
| Fixture JSON deserialization issues with decimal precision | Low | Medium | Use `System.Text.Json` with explicit decimal handling; verify round-trip fidelity |
| Fixture count grows over time creating maintenance burden | Low | Low | Keep to 6-10 golden scenarios; document approval policy in test file comments |
