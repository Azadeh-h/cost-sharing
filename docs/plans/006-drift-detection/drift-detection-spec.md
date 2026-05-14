# Continuous Drift Detection

**Mode**: Simple

ℹ️ Consider running `/plan-1a-explore` for deeper codebase understanding

## Summary

Add continuous drift detection sensors to the cost-sharing app so that codebase health issues — dead code, test coverage gaps, outdated dependencies, and documentation staleness — are caught automatically rather than accumulating silently. Per [Fowler's harness engineering model](https://martinfowler.com/articles/harness-engineering.html), these are "continuous drift and health sensors" that run outside the change lifecycle, monitoring for gradual degradation that no single commit introduces.

## Goals

- **Detect dead code**: Identify unused interfaces, unreferenced services, and orphaned files that add cognitive load without contributing value
- **Measure coverage quality**: Go beyond "tests pass" to track whether test coverage is actually protecting the code that matters (line coverage + mutation score trending)
- **Scan dependencies**: Flag outdated and vulnerable NuGet packages before they become a security or compatibility problem
- **Detect doc drift**: Catch when AGENTS.md, harness.md, or other agent-facing docs state facts that no longer match reality (e.g., "88 tests" when actual count is 91)
- **Provide a single command**: `just drift` runs all sensors and produces a structured report agents can parse and act on
- **Make drift visible**: Output a clear summary showing what's healthy, what's drifting, and what needs attention

## Non-Goals

- **Fixing drift automatically** — sensors detect and report; humans or agents decide what to fix
- **CI/CD integration** — no pipeline exists; drift runs locally on demand
- **Real-time file watching** — this is an on-demand scan, not a daemon
- **Runtime monitoring** — no SLO/latency/error-rate sensors (no deployed backend)
- **Enforcing zero drift** — some drift is tolerable; the goal is visibility, not blocking

## Target Domains

| Domain | Status | Relationship | Role in This Feature |
|--------|--------|-------------|---------------------|
| testing-infrastructure | existing (informal) | **modify** | Add drift detection scripts and justfile recipe |
| algorithms | existing (informal) | **consume** | Scanned for dead code and coverage gaps |
| calculation-services | existing (informal) | **consume** | Scanned for dead code and coverage gaps |

## Complexity

- **Score**: CS-3 (medium)
- **Breakdown**: S=2, I=1, D=0, N=1, F=1, T=1 → Total P=6
  - **Surface Area (S=2)**: Multiple sensors (dead code, coverage, dependencies, doc drift), script, justfile, docs
  - **Integration (I=1)**: External tools (dotnet analyzers, coverlet, `dotnet list package`)
  - **Data/State (D=0)**: No schema or data changes
  - **Novelty (N=1)**: Some ambiguity — which dead code patterns matter? How to parse doc drift?
  - **Non-Functional (F=1)**: Scan duration matters for developer experience
  - **Testing/Rollout (T=1)**: Integration-level — verify each sensor produces meaningful output
- **Confidence**: 0.75
- **Assumptions**:
  - `dotnet list package --outdated` and `--vulnerable` work with the current project structure
  - Coverlet can produce coverage reports without additional config beyond existing `coverlet.collector` reference
  - Dead code detection can be done via grep/analysis without requiring a Roslyn analyzer package
- **Dependencies**:
  - .NET 9 SDK (already confirmed)
  - Coverlet (already referenced in test project)
  - No new external tool installs required for dependency scanning
- **Risks**:
  - Dead code detection via grep may produce false positives (interfaces used only at runtime via DI)
  - Coverage thresholds need baseline data before they can detect regression
  - Doc drift parsing is inherently fragile (regex against markdown)
- **Phases**:
  1. Dependency scanner sensor
  2. Coverage quality sensor (coverlet baseline)
  3. Dead code sensor
  4. Doc drift sensor
  5. Unified `just drift` command with structured output

## Acceptance Criteria

1. **`just drift` command exists** — running it produces a structured report covering all sensors
2. **Dependency scanner reports outdated packages** — lists packages with available updates and flags vulnerable ones
3. **Coverage sensor produces line coverage report** — shows per-file coverage percentage for `CostSharing.Core`
4. **Dead code sensor identifies unused interfaces** — flags interfaces in `CostSharing.Core/Interfaces/` with no test coverage and no direct references in production code
5. **Doc drift sensor checks stated facts** — verifies test count in AGENTS.md matches actual `dotnet test` output; verifies justfile command count matches harness.md documentation
6. **Report is agent-friendly** — output is structured (JSON or clearly parseable sections) so agents can consume and act on findings
7. **Report includes health summary** — each sensor outputs a status (✅ healthy / ⚠️ drifting / ❌ critical) with counts
8. **Existing tests still pass** — `just validate` continues to pass (no production code changes)
9. **AGENTS.md documents `just drift`** — added to Key Commands table with description
10. **Harness.md updated** — drift detection added to Interact section

## Risks & Assumptions

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Dead code false positives (DI-registered services) | High | Medium | Document known false positives; allow exclusion list |
| Coverage baseline too low to be useful | Medium | Low | Record as baseline; value is in tracking trend over time |
| Doc drift regex breaks on formatting changes | Medium | Medium | Keep patterns simple; test against current doc format |
| Scan takes too long (>60s) | Low | Medium | Each sensor is independent; can run selectively |

**Assumptions:**
- No production code changes required — this is purely tooling/scripts
- Drive/Sync/Auth interfaces may appear as "dead code" but are used by the MAUI app (not tested) — need exclusion awareness
- Developer has internet access for `dotnet list package --outdated` (queries NuGet)

## Open Questions

1. [NEEDS CLARIFICATION: Should the dead code sensor only flag interfaces, or also scan for unused services, models, and methods?]
→ **Resolved**: Interfaces only — cleanest signal, fewest false positives in a DI-heavy MAUI app.

2. [NEEDS CLARIFICATION: Should `just drift` produce a single JSON report file, or stream structured output to stdout like the other wrapped commands?]
→ **Resolved**: Stdout stream — consistent with `agent-wrap.sh` pattern.

## Workshop Opportunities

| Topic | Type | Why Workshop | Key Questions |
|-------|------|--------------|---------------|
| Dead Code Heuristics | Other | Determining what counts as "dead" in a DI-heavy MAUI app is nuanced — runtime-registered services aren't referenced statically | Which interfaces are truly orphaned vs. used via DI? Should we whitelist MAUI-only services? |
| Drift Thresholds | Other | When does drift become actionable? Need to define "healthy" vs "drifting" vs "critical" per sensor | What coverage drop triggers ⚠️? How many outdated packages is acceptable? |

---

**Spec Location**: `docs/plans/006-drift-detection/drift-detection-spec.md`

## Testing Strategy

- **Approach**: Lightweight
- **Rationale**: Drift sensors are scripts that scan and report — no production code changes. Verification is that each sensor runs, produces output, and `just validate` still passes.
- **Focus Areas**: `just drift` runs successfully; each sensor produces meaningful output; no false crashes
- **Mock Usage**: N/A — no mocks needed for scanning scripts
- **Excluded**: No unit tests for individual sensor scripts; correctness verified by running them

## Documentation Strategy

- **Location**: AGENTS.md + harness.md only
- **Rationale**: Consistent with mutation testing — agent-facing docs where agents learn available commands

## Clarifications

### Session 2026-05-13

| # | Question | Answer |
|---|----------|--------|
| Q1 | Workflow Mode | **Simple** — single-phase plan, inline tasks |
| Q2 | Testing Strategy | **Lightweight** — verify sensors run and produce output |
| Q3 | Documentation Strategy | **AGENTS.md + harness.md only** |
| Q4 | Dead code scope | **Interfaces only** — cleanest signal, fewest false positives |
| Q5 | Output format | **Stdout stream** — consistent with agent-wrap.sh pattern |
| Q6 | Domain boundaries | **Approved as-is** — testing-infrastructure (modify), others (consume) |
| Q7 | Harness readiness | **Sufficient at L4** — just add `just drift` to harness docs |
