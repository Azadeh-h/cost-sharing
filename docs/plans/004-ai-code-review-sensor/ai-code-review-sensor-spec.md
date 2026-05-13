# AI Code Review Sensor

**Mode**: Simple

## Research Context

📚 This specification incorporates findings from `research-dossier.md`.

**Key research findings**:
- No CI/CD exists — all automation is local via justfile. Sensor must be local-first.
- Current pipeline: planner → implementer → verifier → (manual) auditor. The review sensor fills the gap between verifier and auditor.
- Existing computational sensors (StyleCop, .NET analyzers, 91 xUnit tests) are strong. What's missing is an *inferential* layer that catches semantic/domain issues.
- The harness already uses structured exit codes (0/1/2) and JSON output — the sensor can integrate cleanly.
- Fowler's harness engineering framework identifies this as the "inferential feedback sensor" — the highest-value missing piece in a mature harness.

## Summary

Add an **automated AI code review sensor** that runs after every successful validation (`just validate`) and catches logic errors, domain violations, boundary breaks, and pattern inconsistencies that computational sensors (tests, linters) cannot detect.

**WHY**: Currently, semantic review happens only when a human manually invokes the auditor. This means code changes can reach the repository without domain-aware review. An automated inferential sensor closes this gap — reducing human review toil and catching issues before they accumulate.

## Goals

- Every code change is automatically reviewed for semantic correctness after validation passes
- Domain invariants (expense conservation, balance preservation, settlement correctness, deterministic rounding, no impossible states) are checked beyond what unit tests cover
- Boundary violations are detected (e.g., Core referencing MAUI types, services bypassing interfaces)
- Pattern breaks are flagged (e.g., manual instantiation where DI is expected)
- Review findings are structured and actionable — written as fix-instructions the implementing agent can act on (Fowler's "positive prompt injection")
- The sensor integrates into the existing `scripts/harness.sh` pipeline without breaking the current flow
- Non-blocking by default — findings are reported but don't fail the pipeline until confidence is established

## Non-Goals

- **Replacing human review** — the sensor augments, not replaces, human judgment for complex decisions
- **Duplicating computational sensors** — no style checking (StyleCop does this), no null warnings (.NET analyzers), no test failures (xUnit)
- **CI/CD pipeline** — this is local-first. GitHub Actions integration is a future enhancement
- **Full codebase scanning** — the sensor reviews only *changed* files, not the entire repository
- **Auto-fixing code** — the sensor reports findings; it does not modify files
- **Performance profiling** — not a runtime sensor; operates on static code analysis
- **Confidence categorization of findings** — not needed for v1; all findings treated equally

## Target Domains

| Domain | Status | Relationship | Role in This Feature |
|--------|--------|-------------|---------------------|
| harness-infra | **NEW** | **create** | The review sensor is a new component in the harness infrastructure |
| agent-pipeline | **NEW** | **modify** | Extends the multi-agent execution order with a new reviewer role |

### New Domain Sketches

#### harness-infra [NEW]
- **Purpose**: Infrastructure for the agent feedback loop — build, test, review, and observe automation via justfile recipes and shell scripts.
- **Boundary Owns**: justfile recipes, scripts/harness.sh, evidence capture, self-healing logic, exit code conventions, environment diagnostics (doctor)
- **Boundary Excludes**: Agent definitions (owned by agent-pipeline), domain business logic (owned by expense-management), test assertions (owned by test suite)

#### agent-pipeline [NEW — formalization of existing concept]
- **Purpose**: Orchestration of multi-agent workflows — defines agent roles, execution order, output contracts, and stop conditions.
- **Boundary Owns**: Agent definitions (.github/agents/*.agent.md), execution order (planner→implementer→verifier→reviewer→auditor), output format contracts, AGENTS.md bootstrap guide
- **Boundary Excludes**: Individual agent logic (each agent owns its own behavior), harness infrastructure (owned by harness-infra), domain invariants (owned by expense-management)

## Complexity

- **Score**: CS-2 (small)
- **Breakdown**: S=1, I=1, D=0, N=1, F=0, T=1 (Total P=4)
- **Confidence**: 0.80
- **Assumptions**:
  - The sensor uses `git diff` to identify changed files (no external tooling required)
  - Review is performed by an LLM invoked via the existing agent infrastructure (no new API keys or services)
  - The sensor is a shell script + agent definition — no compiled code
- **Dependencies**:
  - Existing justfile and harness.sh infrastructure (stable)
  - An LLM available in the agent context (already present — this runs within Copilot/agent sessions)
- **Risks**:
  - Non-deterministic output (LLM reviews may vary between runs)
  - False positives could erode trust if too noisy initially
- **Phases**:
  1. Agent definition + justfile recipe (manual invocation)
  2. Harness.sh integration (automatic after validate)

## Acceptance Criteria

1. **Given** a code change has been made, **When** I run `just review`, **Then** I receive a structured review output listing findings (or "no findings") with severity, file, line, category, and actionable suggestion
2. **Given** `just validate` passes in `scripts/harness.sh`, **When** the harness continues, **Then** the review sensor runs automatically before the final result is reported
3. **Given** the review sensor finds a critical domain violation, **When** the output is displayed, **Then** the finding includes a fix-instruction written for LLM consumption (not just a description of the problem)
4. **Given** the review sensor runs on a change that only modifies test files, **When** review completes, **Then** domain invariant checks are skipped (tests don't affect domain logic)
5. **Given** the review sensor encounters an error (LLM unavailable, timeout), **When** the error occurs, **Then** the harness continues gracefully (non-blocking) and logs a warning
6. **Given** no files have changed since last review, **When** `just review` is invoked, **Then** it reports "nothing to review" and exits 0
7. **Given** a `.github/agents/reviewer.agent.md` exists, **When** a new agent reads it, **Then** it understands its scope, output format, and what NOT to review (computational sensor territory)
8. **Given** the review produces findings, **When** the output is written, **Then** structured evidence is saved to `scratch/evidence/review-{timestamp}.json`

## Risks & Assumptions

### Risks
- **R1: False positive noise** — LLM may flag correct code as problematic. Mitigation: start non-blocking, tune prompt over time based on false positive rate.
- **R2: Non-determinism** — Same code may get different reviews on different runs. Mitigation: accept as inherent to inferential sensors; focus on high-confidence findings only.
- **R3: Latency** — LLM review adds time to the pipeline. Mitigation: review only changed files (not full codebase), set timeout.

### Assumptions
- **A1**: The review sensor runs within an existing agent/LLM context (no separate API keys needed)
- **A2**: `git diff` reliably identifies changed files for review scope
- **A3**: Non-blocking is acceptable for v1 — the sensor warns but doesn't fail the pipeline
- **A4**: The existing JSON output format and exit code conventions are sufficient

## Open Questions

All open questions resolved — see Clarifications below.

## Testing Strategy

- **Approach**: Lightweight
- **Rationale**: The sensor is a shell script + agent definition (no compiled code). A smoke test confirming `just review` exits 0 and produces valid JSON is sufficient.
- **Focus Areas**: Script exit codes, JSON output structure, graceful timeout handling
- **Mock Usage**: N/A (no external systems to mock)
- **Excluded**: No unit tests for prompt content (inferential, non-deterministic)

## Documentation Strategy

- **Location**: No new documentation — agent definition (`reviewer.agent.md`) is self-documenting; `AGENTS.md` update covers usage.
- **Rationale**: CS-2 feature with clear conventions. The agent definition IS the documentation.

## Clarifications

### Session 2026-05-13

- Q: Workflow Mode? → A: Simple (pre-set in spec — CS-2, single-phase viable)
- Q: Testing Strategy? → A: Lightweight — smoke test confirming `just review` exits 0 with valid JSON
- Q: Review scope — all files or only allowed write scope? → A: All changed files (catches issues everywhere, not just agent-writable areas)
- Q: Timeout before giving up? → A: 30 seconds (keeps pipeline snappy; graceful fallback on timeout)
- Q: Domain boundaries — keep separate harness-infra + agent-pipeline? → A: Keep both — useful to separate infrastructure from orchestration
- Q: Harness readiness — is L4 sufficient? → A: Yes, L4 is sufficient — just build the sensor and integrate it

## Workshop Opportunities

| Topic | Type | Why Workshop | Key Questions |
|-------|------|--------------|---------------|
| Review prompt engineering | Other | The quality of the sensor depends entirely on how the review prompt is crafted. What instructions produce high-signal, low-noise findings? | What context to include? How to frame domain invariants? How to suppress noise? |
