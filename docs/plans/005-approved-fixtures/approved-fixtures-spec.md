# Approved Fixtures for Financial Determinism

**Mode**: Simple

📚 This specification incorporates findings from research-dossier.md

## Research Context

The research dossier identified that the current 91-test suite uses a mix of exact-value assertions (fixture-like) and property-only assertions (sum conservation, count bounds). The highest-value gaps are:

1. **Rounding tests** — 3 split tests verify sum conservation but not which participant receives the extra cent
2. **Debt simplification** — 4 tests use inequality bounds (`count <= 2`, `total <= 50`) instead of exact transaction routing
3. **End-to-end pipeline** — no test covers the full expense → split → debt → simplification → settlement flow

The pattern draws from Martin Fowler's harness engineering article, where approved fixtures serve as a **behavioral harness** — committed golden outputs that make exact financial outcomes auditable and reviewable.

## Summary

Add approved fixtures (golden master tests) for the Cost Sharing App's critical financial scenarios. Pre-computed expected outputs are committed as JSON fixture files. Tests compare actual service outputs against these fixtures exactly. When behavior intentionally changes, the fixture diff is explicitly reviewed and approved by a human — making financial behavior changes visible, auditable, and intentional.

This strengthens the existing L4 harness by adding a **behavior preservation sensor** alongside the existing computational sensors (unit tests) and inferential sensor (AI reviewer).

## Goals

- Lock down deterministic rounding behavior so the exact cent-allocation policy is committed and reviewable
- Verify debt simplification produces a canonical transaction set, not just "fewer transactions"
- Provide end-to-end golden scenarios that catch integration drift between split, debt, and settlement services
- Make financial behavior changes visible as data diffs in code review (JSON fixture changes)
- Complement the auditor agent's 5 domain invariants with exact behavioral verification

## Non-Goals

- Replacing existing unit tests — approved fixtures augment, not replace
- Testing non-financial logic (UI, auth, invitations)
- Adding property-based/fuzzing tests (separate concern)
- Enforcing a specific fixture approval workflow tooling (policy is documented, not automated)
- Changing any existing service logic — this is purely additive test infrastructure

## Target Domains

| Domain | Status | Relationship | Role in This Feature |
|--------|--------|-------------|---------------------|
| financial-core | **NEW** | **consume** | All fixture scenarios exercise split, debt, and simplification services |

### New Domain Sketches

#### financial-core [NEW]
- **Purpose**: Encapsulates expense splitting, debt calculation, debt simplification, and settlement logic — the business-critical financial pipeline
- **Boundary Owns**: SplitCalculationService, DebtCalculationService, DebtSimplificationAlgorithm, settlement balance updates
- **Boundary Excludes**: UI/ViewModel layer (presentation), persistence/SQLite (infrastructure), auth/invitations (separate concerns)

ℹ️ No domain registry exists. Consider running `/plan-v2-extract-domain` to formalize the financial-core domain.

## Complexity

- **Score**: CS-2 (small)
- **Breakdown**: S=1, I=0, D=0, N=1, F=1, T=1
  - S=1: Multiple test files modified + new fixture directory and files
  - I=0: Purely internal, no external dependencies
  - D=0: No schema or data changes
  - N=1: Canonicalization question for debt simplification (do multiple valid outputs exist?)
  - F=1: Financial precision requirements — fixtures must be exact to the cent
  - T=1: Integration-style end-to-end fixture tests added alongside unit tests
- **Confidence**: 0.85
- **Assumptions**:
  - The Min-Cash-Flow algorithm produces deterministic output for a given input ordering
  - Existing service interfaces remain stable
  - JSON is a suitable fixture format for this team
- **Dependencies**: None — purely additive to existing test infrastructure
- **Risks**:
  - If debt simplification is non-deterministic (multiple valid outputs), fixtures need normalization logic
  - Fixture maintenance burden if business rules change frequently
- **Phases**:
  - Phase 1: Rounding fixtures (strengthen 3 existing weak tests)
  - Phase 2: Simplification fixtures (lock down 4 debt simplification scenarios)
  - Phase 3: End-to-end pipeline fixture (1-2 golden scenarios)

## Acceptance Criteria

1. A `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/` directory exists containing JSON golden files for financial scenarios
2. The 3 rounding tests (`ThreeMembers`, `UnevenDivision`, `DecimalPercentages`) assert exact per-person amounts, not just sum conservation
3. Debt simplification tests for circular and complex scenarios assert exact transaction routing (from → to → amount), not just count bounds
4. At least one end-to-end fixture covers: expense input → split → debt calculation → simplification → post-settlement remainder
5. All approved fixture tests pass with the current service implementations (no logic changes needed)
6. Fixture JSON files are human-readable and produce clear diffs when behavior changes
7. The rounding gauntlet scenario (`$1/3`, `$10/3`, `$100/3`, `$99.99/7`) has committed exact expected outputs

## Risks & Assumptions

- **Risk**: The Min-Cash-Flow algorithm may have multiple valid outputs for some inputs. If so, either the algorithm must be made deterministic (stable sort, canonical creditor ordering) or fixture comparison must normalize before asserting.
  - **Mitigation**: Run the algorithm on test inputs first to verify determinism before committing fixtures.
- **Risk**: Fixture maintenance overhead when business rules evolve.
  - **Mitigation**: Keep fixture count small (6-10 golden scenarios). Document approval policy.
- **Assumption**: Current service implementations produce correct outputs that should be locked down as the golden standard.
- **Assumption**: JSON deserialization in xUnit is straightforward with System.Text.Json.

## Open Questions

All open questions resolved in clarification session 2026-05-14.

## Testing Strategy

- **Approach**: Lightweight
- **Rationale**: This feature IS test infrastructure — validation is running `just validate` and confirming all fixture tests pass alongside existing 91 tests
- **Focus Areas**: Fixture correctness (exact value matching), JSON deserialization, determinism verification
- **Mock Usage**: None — real service instances only, no mocks
- **Excluded**: No additional test layers beyond xUnit

## Documentation Strategy

- **Location**: No new documentation
- **Rationale**: Internal test infrastructure — fixture files and test code are self-documenting

## Clarifications

### Session 2026-05-14

**Q1 — Workflow Mode**: Simple (confirmed, CS-2 scope)

**Q2 — Testing Strategy**: Lightweight — run `just validate` with new fixture tests, no additional layers

**Q3 — Fixture File Location**: Inside existing test project at `CostSharingApp/tests/CostSharingApp.Tests/ApprovedFixtures/` (co-located, simple)

**Q4 — Min-Cash-Flow Determinism**: Verify determinism first — run algorithm on test inputs, confirm same output every time, then commit exact outputs as fixtures. If non-deterministic, address before committing fixtures.

**Q5 — Domain & Harness**: financial-core domain boundary accepted as-is. L4 harness sufficient — no changes needed.

## Workshop Opportunities

| Topic | Type | Why Workshop | Key Questions |
|-------|------|--------------|---------------|
| Canonical simplification | Data Model | Multiple valid debt simplifications may exist — need to decide if algorithm enforces one canonical form or if fixtures normalize before comparison | What ordering rules make simplification deterministic? How to normalize equivalent transaction sets? |
