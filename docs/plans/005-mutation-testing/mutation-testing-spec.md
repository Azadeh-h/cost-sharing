# Add Mutation Testing

**Mode**: Simple

📚 This specification incorporates findings from `docs/plans/004-ai-code-review-sensor/research-mutation-testing.md`

## Research Context

Research (68 findings, 8 subagents) established that:
- `CostSharing.Core` is a pure `net9.0` library — ideal mutation target with zero MAUI dependencies
- 75 tests across 6 classes exist but several use broad assertions that would let mutants survive
- No mutation testing, coverage thresholds, or CI pipeline exist today
- 3 primary mutation targets identified: `SplitCalculationService`, `DebtCalculationService`, `DebtSimplificationAlgorithm`
- Prior plan (004) explicitly flagged mutation testing as a missing "inferential sensor"

## Summary

Add mutation testing to the cost-sharing app so that developers and agents can measure how effective the existing test suite is at catching real bugs — not just whether tests pass. Financial calculation code (splits, debts, settlements) demands correctness guarantees beyond line coverage. Mutation testing introduces small, systematic changes to production code and checks whether tests detect them, revealing assertion gaps and undertested logic paths.

## Goals

- **Measure test effectiveness**: Produce a mutation score showing what percentage of code changes (mutants) the test suite catches
- **Reveal weak assertions**: Identify tests that pass despite meaningful code changes (surviving mutants)
- **Protect financial correctness**: Ensure the split calculation, debt calculation, and debt simplification algorithms have tests strong enough to catch off-by-one, sign-flip, and boundary errors
- **Make mutation testing a routine quality check**: Integrate into the justfile so `just mutate` runs alongside `just test` and `just validate`
- **Establish a mutation score baseline**: Record the initial score so future changes can be compared against it
- **Document for agents**: Update AGENTS.md and harness so AI agents can run and interpret mutation testing results

## Non-Goals

- **Mutating MAUI app code** — the MAUI project has no test coverage; mutation testing there would produce only noise
- **Achieving 100% mutation score** — the goal is visibility into test quality, not perfection; a realistic initial target is 70%+
- **Adding new tests in this feature** — mutation testing will *reveal* where tests are weak, but strengthening assertions is a follow-up activity
- **CI/CD integration** — no pipeline exists today; CI integration is a separate future feature
- **Property-based testing** — recommended by prior research but out of scope for this feature (can complement mutation testing later)
- **Mutating model-only code** — property getters/setters generate noise with low signal

## Target Domains

| Domain | Status | Relationship | Role in This Feature |
|--------|--------|-------------|---------------------|
| testing-infrastructure | **NEW** | **create** | Houses mutation testing tooling, config, and justfile recipes |
| algorithms | existing (informal) | **consume** | Primary mutation target — DebtSimplificationAlgorithm |
| calculation-services | existing (informal) | **consume** | Primary mutation target — SplitCalculation, DebtCalculation |

### New Domain Sketches

#### testing-infrastructure [NEW]
- **Purpose**: Owns test quality tooling beyond basic xUnit: mutation testing configuration, coverage thresholds, quality gate definitions, and harness integration for automated quality checks.
- **Boundary Owns**: Stryker.NET config, mutation score baselines, justfile test/quality recipes, test quality documentation in AGENTS.md/harness.md
- **Boundary Excludes**: Test code itself (owned by the domain being tested), CI/CD pipeline config (separate concern), production code changes

## Complexity

- **Score**: CS-2 (small)
- **Breakdown**: S=1, I=1, D=0, N=0, F=1, T=1 → Total P=4
  - **Surface Area (S=1)**: Multiple files touched (stryker-config, justfile, AGENTS.md, harness.md, tool manifest) but all config/docs
  - **Integration (I=1)**: One external dependency (Stryker.NET dotnet tool)
  - **Data/State (D=0)**: No schema or data changes
  - **Novelty (N=0)**: Well-specified — Stryker.NET is a mature tool with clear documentation
  - **Non-Functional (F=1)**: Moderate — mutation testing run time affects developer experience; threshold tuning matters
  - **Testing/Rollout (T=1)**: Integration-level — must verify Stryker runs correctly against existing test suite
- **Confidence**: 0.85
- **Assumptions**:
  - Stryker.NET supports `net9.0` target framework
  - Stryker.NET works with xUnit 2.9.2 and Microsoft.NET.Test.Sdk 17.12.0
  - `CostSharing.Core` can be mutated independently without the MAUI project
- **Dependencies**:
  - Stryker.NET NuGet tool package availability
  - .NET 9 SDK installed (already confirmed in harness)
- **Risks**:
  - Stryker.NET may not yet support .NET 9 (mitigated: research opportunity identified)
  - Initial mutation score may be discouraging (~60-75%) due to weak assertions — this is expected and valuable
- **Phases**:
  1. Install Stryker.NET and create configuration
  2. Run initial baseline on DebtSimplificationAlgorithm
  3. Expand scope to all Tier 1 targets
  4. Integrate into justfile and update documentation

## Acceptance Criteria

1. **Stryker.NET is installed as a local dotnet tool** — running `dotnet stryker` from the `CostSharingApp/` directory produces a mutation testing report
2. **Configuration targets `CostSharing.Core` only** — the MAUI app project is excluded from mutation scope
3. **A mutation report is generated** — HTML report showing mutation score, surviving mutants, and killed mutants for each source file
4. **`just mutate` command exists** — running `just mutate` in the repo root executes Stryker and produces the report
5. **Mutation score baseline is recorded** — the initial mutation score is documented (expected: 60-80%)
6. **Thresholds are configured** — Stryker is configured with break/low/high thresholds so future score regressions are visible
7. **AGENTS.md is updated** — documents the `just mutate` command, expected score range, and how to interpret results
8. **Harness.md is updated** — adds mutation testing to the Interact section and quality gates
9. **Existing tests still pass** — `just validate` continues to pass (no production code changes)
10. **Report is gitignored** — Stryker output directory (`StrykerOutput/`) is added to `.gitignore`

## Risks & Assumptions

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Stryker.NET doesn't support .NET 9 | Low | High | Check latest version compatibility; fallback to running against net8.0 target if needed |
| Initial mutation score is very low (<50%) | Medium | Low | Expected — this is the value proposition; document as baseline |
| Mutation testing is slow (>5 min) | Medium | Medium | Scope to Tier 1 targets only; configure concurrent test runners |
| Stryker conflicts with MAUI build | Low | Medium | Config excludes MAUI project; test project targets net9.0 only |

**Assumptions:**
- The developer has .NET 9 SDK installed (verified by harness)
- Internet access is available for initial `dotnet tool install`
- No production code changes are required — this is purely tooling/config

## Testing Strategy

- **Approach**: Lightweight
- **Rationale**: This feature adds tooling and config only — no production code changes. Verification is that Stryker installs, runs against `CostSharing.Core`, and produces a mutation report.
- **Focus Areas**: `just mutate` runs successfully and returns exit code 0 when score ≥ 60%; `just validate` continues to pass
- **Mock Usage**: N/A — no mocks needed for tooling/config changes
- **Excluded**: No unit tests for Stryker config itself; Stryker's own correctness is its responsibility

## Documentation Strategy

- **Location**: AGENTS.md + harness.md only
- **Rationale**: Mutation testing is primarily an agent/developer workflow concern; AGENTS.md is where agents learn available commands, harness.md is where the quality loop is defined

## Open Questions

~~1. Should mutation testing results (HTML reports) be committed to the repo or remain local-only?~~
→ **Resolved**: Gitignore reports. Generated locally on demand via `just mutate`.

~~2. What mutation score threshold should break the build?~~
→ **Resolved**: 60% break threshold (lenient start). Can ratchet up as assertions improve.

## Workshop Opportunities

| Topic | Type | Why Workshop | Key Questions |
|-------|------|--------------|---------------|
| Mutation Score Thresholds | Other | Choosing break/low/high thresholds affects developer experience — too strict blocks work, too lenient provides no signal | What score is realistic for financial code with known weak assertions? Should thresholds ratchet up over time? |

---

**Spec Location**: `docs/plans/005-mutation-testing/mutation-testing-spec.md`
**Research**: `docs/plans/004-ai-code-review-sensor/research-mutation-testing.md`

## Clarifications

### Session 2026-05-13

| # | Question | Answer |
|---|----------|--------|
| Q1 | Workflow Mode | **Simple** — CS-2 task, single-phase plan, inline tasks |
| Q2 | Testing Strategy | **Lightweight** — verify Stryker installs and runs; `just validate` + `just mutate` both work |
| Q3 | Documentation Strategy | **AGENTS.md + harness.md only** — keep docs where agents reference them |
| Q4 | Mutation reports storage | **Gitignore** — reports generated locally on demand, repo stays clean |
| Q5 | Break threshold | **60%** — lenient start; ratchet up as assertions improve |
| Q6 | Domain boundaries | **Approved as-is** — `testing-infrastructure` (NEW), algorithms + calculation-services (consume) |
| Q7 | Harness readiness | **Sufficient at L4** — just add `just mutate` to harness docs |
