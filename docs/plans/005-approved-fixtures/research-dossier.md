# Research Report: Approved Fixtures for Financial Determinism

**Generated**: 2026-05-14T00:00:00Z
**Research Query**: "Introduce approved fixtures for business-critical financial behavior"
**Mode**: Pre-Plan
**Location**: docs/plans/005-approved-fixtures/research-dossier.md
**FlowSpace**: Not Available
**Findings**: 24 total

## Executive Summary

### What It Does
This research explores adding an **approved fixtures** testing pattern to the Cost Sharing App’s financial test suite. In Fowler’s harness engineering framing, approved fixtures are repo-committed golden outputs for critical scenarios. Tests compare runtime results against those fixtures exactly, and any intentional behavior change requires a human to explicitly approve the updated fixture.

### Business Purpose
Approved fixtures strengthen confidence in deterministic financial behavior — especially rounding, debt routing, and end-to-end settlement flows where “close enough” is not sufficient. They make behavior auditable, reviewable, and harder for accidental changes or AI-generated code to silently alter.

### Key Insights
1. **Approved fixtures are a behavioral harness tool** — they verify exact outcomes, not just invariants or structural properties.
2. **The current suite already contains partial fixture-style tests** — many split and debt tests assert exact values, so this is an extension of an existing testing style, not a foreign pattern.
3. **The biggest gap is debt simplification** — the Min-Cash-Flow algorithm is the most behavior-sensitive financial logic, yet several tests only assert bounds like transaction count or conserved totals.
4. **Rounding scenarios are under-specified** — tests for `$100 / 3`, `$10 / 3`, and decimal percentages currently verify sum conservation but not which participant receives the extra cent.
5. **End-to-end golden scenarios are missing** — there is no repo-committed fixture that locks down the full path from expense input to simplified debts and post-settlement remainder.

### Quick Stats
- **Harness level**: L4 (self-healing)
- **Domain registry**: None
- **Test runtime**: ~25ms
- **Analysis snapshot**: 91 tests total
- **Auditor coverage**: 5 domain invariants checked manually
- **Highest-value fixture targets**: split rounding, debt simplification routing, full-pipeline scenarios

## How It Currently Works

### Current Testing Style
The current suite uses a mixed strategy:
- **Exact assertions** for many straightforward financial cases
- **Property assertions** for several rounding and simplification cases
- **Regression tests** for known split bugs in view-model coverage

This means the codebase already values determinism, but applies it inconsistently in the places where deterministic behavior matters most.

### Split Calculation Coverage
`SplitCalculationServiceTests` contains 16 tests and is split between strong and weak behavioral verification.

#### Already fixture-like (exact values)
- `CalculateEvenSplit_TwoMembers_SplitsEvenly` — exactly 50m each
- `CalculateEvenSplit_OneMember_GetsFullAmount` — exactly 100m
- `CalculateCustomSplit_ValidPercentages_CalculatesCorrectly` — exactly 75m, 45m, 30m
- `CalculateCustomSplit_ZeroPercentage_ExcludesMember` — exactly 60m, 40m
- `CalculateCustomSplit_LargeAmount_MaintainsPrecision` — exactly 400000m, 350000m, 250000m

#### Not yet fixture-like (property-only)
- `CalculateEvenSplit_ThreeMembers_SplitsEvenlyWithRounding` — checks only total sum = 100
- `CalculateEvenSplit_UnevenDivision_RoundsToTwoCents` — checks only total sum = 10
- `CalculateCustomSplit_DecimalPercentages_RoundsToTwoCents` — checks only total sum = 100

These three tests are the clearest approved-fixture candidates because they exercise remainder allocation and rounding determinism.

### Debt Calculation Coverage
`DebtCalculationServiceTests` is already much closer to approved-fixture style.

Examples of existing exact assertions:
- `MultipleExpenses_AggregatesDebts` — exactly $20 net debt
- `WithSettlements_ReducesDebts` — exactly $20 remaining
- `MultipleCreditors_DistributesDebtCorrectly` — exact Alice=$60, Dave=$60 routing

This area shows the pattern works well in the repository today when outputs are explicitly asserted.

### Debt Simplification Coverage
`DebtSimplificationAlgorithmTests` is the weakest area for behavioral lock-down.

#### Current weak assertions
- `ThreePersonCircular_SimplifiesToTwo` — asserts `count <= 2`, `total <= 50`
- `FourPersonComplex_ReducesTransactions` — asserts transaction count shrinks, but not to exact routing
- `LargeAmounts_MaintainsPrecision` — asserts totals and count bounds only
- `BalancesZeroOut_PreservesTotalAmounts` — checks equality of totals, not exact simplified debts

These tests verify that outputs remain plausible, but not that the algorithm remains deterministic in its exact transaction choices.

### Additional ViewModel Regression Coverage
`CustomSplitViewModelTests` adds more exact split-value coverage, including bug-fix regressions such as:
- `ZeroParticipantWithIndivisibleAmount`
- `ZeroParticipantWithOddCents`

That indicates the repo already has a practical need for exact-value behavioral preservation when edge cases are discovered.

## Architecture & Design

### What “Approved Fixtures” Means Here
For this codebase, approved fixtures should mean:
1. **Golden master outputs** are committed for critical financial scenarios.
2. Tests compare actual outputs to those fixtures **exactly**.
3. Intentional behavior changes require a human to re-approve the fixture.
4. Fixtures capture business-critical, user-visible calculation behavior rather than just internal structure.
5. The pattern acts as a behavioral harness layer above ordinary unit tests and invariants.

### Why This Fits Financial Logic
Financial systems need more than invariant preservation. It is not enough that:
- totals sum correctly,
- balances net to zero,
- and debt count decreases.

Users also expect **stable, auditable, deterministic answers**:
- who receives the extra cent,
- which creditor gets paid first,
- which simplified transactions appear after optimization,
- and what remains after a settlement is applied.

Approved fixtures make those answers explicit and reviewable.

### Proposed Fixture Architecture
**Recommended structure:** `tests/ApprovedFixtures/`

**Recommended format:** JSON golden files

Why JSON is a good fit:
- easy for humans and agents to diff in code review,
- easy to deserialize in tests,
- natural for arrays of splits, balances, debts, and settlements,
- keeps expected outputs separate from test code so approvals are visible as artifact changes.

### Suggested Fixture Scope
#### Layer 1: Targeted service fixtures
- split rounding
- custom split rounding
- debt simplification routing

#### Layer 2: End-to-end pipeline fixtures
- expense input
- participant allocations
- computed debts
- simplified debts
- post-settlement remaining debts

#### Layer 3: Canonical edge-case bundle
A small “rounding gauntlet” fixture suite that locks down cent-level allocation rules across repeated edge cases.

### Determinism Requirement
Approved fixtures work best when the underlying algorithm produces a canonical output. That matters most for debt simplification: if multiple valid simplifications exist, the team must decide whether to:
- enforce a stable canonical ordering/routing rule, or
- encode a fixture comparison that normalizes equivalent outputs first.

That question is the main design workshop item for this effort.

## Quality & Testing

### Why Existing Tests Are Not Enough
Property-based assertions are useful but insufficient for business-critical finance logic.

Examples:
- A split test that only checks `sum == 100` will pass for `[33.34, 33.33, 33.33]`, `[33.33, 33.34, 33.33]`, and `[33.33, 33.33, 33.34]`.
- A simplification test that only checks `count <= 2` allows many different outputs, some stable and some not.

From a user-trust perspective, these are materially different behaviors even when invariants hold.

### Gap Analysis

#### Gap 1: Rounding behavior (HIGH)
Exact expected outcomes are missing for the scenarios where rounding policy matters most.

Examples that should be locked down:
- `$100 / 3` → `[33.34, 33.33, 33.33]`
- `$10 / 3` → `[3.34, 3.33, 3.33]`

Current tests prove conservation of value, but not deterministic remainder assignment.

#### Gap 2: Debt simplification routing (CRITICAL)
The most complex financial logic in the app currently has the loosest assertions.

The four-person scenario shows why this matters: current tests accept “fewer transactions,” but approved fixtures would specify the exact simplified result. Without that, routing can change silently while still satisfying all current assertions.

#### Gap 3: End-to-end golden scenarios (HIGH)
No fixture currently protects the full computation pipeline. Integration drift between split calculation, debt calculation, simplification, and settlements could therefore pass unit tests if each component still looks locally plausible.

#### Gap 4: Circular debt resolution (MEDIUM)
The circular-debt case already has a well-defined net outcome, making it ideal for a committed golden fixture.

### Concrete Golden Scenarios

#### Scenario 1: The Three-Way Split
**Input**: `$100.00` evenly among 3 people  
**Expected**: `[33.34, 33.33, 33.33]`

#### Scenario 2: The Indivisible Ten
**Input**: `$10.00` evenly among 3 people  
**Expected**: `[3.34, 3.33, 3.33]`

#### Scenario 3: The Circular Debt
**Input**: `A→B $50, B→C $30, C→A $20`  
**Net balances**: `A=-30, B=+20, C=+10`  
**Expected simplified**: `A→B $20, A→C $10`

#### Scenario 4: The Complex Four-Person
**Input**: `A→B $40, A→C $20, B→C $30, B→D $10, C→D $50, D→A $30`  
**Net balances**:
- A = -30
- B = 0
- C = 0
- D = +30

**Expected simplified**: `A→D $30`

This is especially valuable because the current test allows up to three transactions even though the balances collapse to one obvious result.

#### Scenario 5: The Full Pipeline
**Input**:
- Group of 3
- Alice pays `$150` dinner, even split
- Bob pays `$90` taxi, custom split `60/40` between Alice and Bob

**Expected split outputs**:
- Dinner: Alice=`50`, Bob=`50`, Charlie=`50`
- Taxi: Alice=`54`, Bob=`36`

**Expected balances**:
- Alice: paid `150`, owes `104`, net `+46`
- Bob: paid `90`, owes `86`, net `+4`
- Charlie: paid `0`, owes `50`, net `-50`

**Expected debts**:
- Charlie→Alice `46`
- Charlie→Bob `4`

**Expected post-settlement remainder** after Charlie pays Alice `46`:
- Charlie→Bob `4`

#### Scenario 6: The Rounding Gauntlet
Bundle multiple edge cases into a single explicit approval surface:
- `$1.00 / 3` → `[0.34, 0.33, 0.33]`
- `$0.01 / 2` → deterministic single-cent allocation
- `$0.03 / 2` → deterministic two-way remainder policy
- `$99.99 / 7` → exact committed output

This scenario is useful because it forces the team to state its cent-allocation policy unambiguously.

### Harness Relevance
Approved fixtures complement the current L4 harness rather than replace it.

- Existing tests remain the fast computational sensor.
- The auditor remains the domain-invariant sensor.
- Approved fixtures become the **behavior preservation sensor** that detects exact-output drift.

This is especially relevant for AI-assisted development, where code can preserve invariants while still changing exact user-visible outcomes.

## Critical Discoveries

1. **The repository already uses approved-fixture thinking in places.** Exact-value assertions exist across split, debt, and regression tests, so the pattern is culturally compatible.
2. **The highest-value fixture targets are the least exact tests.** The most important deterministic cases — rounding and simplification routing — are precisely where current assertions are weakest.
3. **Debt simplification is the largest risk surface.** The Min-Cash-Flow algorithm is complex enough that “valid but different” outputs are easy to introduce accidentally.
4. **Canonicalization is the main design issue.** Approved fixtures for simplification require either deterministic routing or an agreed normalization strategy.
5. **End-to-end fixtures would close an integration blind spot.** Current tests are strong at the unit level but do not commit full business scenarios as golden artifacts.
6. **JSON fixtures would make approvals visible in code review.** Reviewers can inspect behavior changes directly as data diffs instead of inferring them from altered assertions.
7. **The reviewer agent should treat fixture changes as sensitive artifacts.** If approved fixtures change, review automation should flag that a business behavior contract changed and deserves explicit human attention.

## Recommendations

### 1. Create a dedicated fixture directory
Add `tests/ApprovedFixtures/` for golden scenario artifacts.

### 2. Start with the three rounding tests
Convert these property-only tests to exact-output verification first:
- `CalculateEvenSplit_ThreeMembers_SplitsEvenlyWithRounding`
- `CalculateEvenSplit_UnevenDivision_RoundsToTwoCents`
- `CalculateCustomSplit_DecimalPercentages_RoundsToTwoCents`

This is the lowest-risk, highest-signal first step.

### 3. Lock down debt simplification outputs
Formalize exact fixtures for:
- circular debt resolution,
- complex four-person routing,
- large-amount simplification.

This is the most important trust and determinism upgrade.

### 4. Add 2–3 end-to-end golden scenarios
Prioritize scenarios that exercise multiple services together:
- mixed even/custom splits,
- settlement application,
- multi-expense aggregation.

### 5. Prefer JSON over inline expected values
Use data files instead of long inline assertions so approvals are easy to review, diff, and regenerate intentionally.

### 6. Integrate fixture awareness into review automation
The AI code review sensor should flag when approved fixture files change, because those diffs represent changes to expected business behavior, not just test maintenance.

### 7. Establish an approval policy
Document who can approve fixture changes, what evidence is required, and how rationale should be recorded when golden outputs are intentionally updated.

## Next Steps

### Immediate
1. Create the `tests/ApprovedFixtures/` folder structure.
2. Encode the three rounding scenarios as JSON fixtures.
3. Update the corresponding tests to compare exact outputs against fixture data.

### Near-Term
4. Add approved fixtures for circular debt and complex four-person simplification.
5. Create one full-pipeline golden scenario covering split → debt calculation → simplification → settlement.
6. Teach the reviewer flow to highlight approved-fixture changes.

### Workshop Topics
- **Canonical simplification**: If multiple valid debt simplifications exist, what output form is considered the approved one?
- **Fixture representation**: JSON files vs inline theory data vs embedded resources.
- **Approval workflow**: Who approves fixture changes, and how should rationale be recorded?

### Definition of Success
This effort is successful when the repository can answer all of the following with committed evidence:
- Who gets the extra cent?
- Which simplified transaction set is canonical?
- What exact debts remain after settlement?
- Which fixture changed, and who approved the new business behavior?

At that point, approved fixtures become a practical behavior harness for the app’s most important financial logic.