# Research Report: AI Code Review Sensor (Post-Change Automation)

**Generated**: 2026-05-13T11:35:00+10:00
**Research Query**: "Add AI code review agent as a post-change sensor that runs automatically"
**Mode**: Pre-Plan
**Location**: docs/plans/004-ai-code-review-sensor/research-dossier.md
**FlowSpace**: Not Available
**Findings**: 28 total

## Executive Summary

### What It Does
This research explores adding an **automated AI code review sensor** to the existing L4 agent harness. Per Fowler's harness engineering framework, this fills the "inferential feedback sensor" gap — currently the auditor agent is invoked manually, meaning code changes can reach the repo without automated domain-aware review.

### Business Purpose
Reduce human review toil by catching domain violations, logic errors, and architectural drift automatically after every code change — before changes reach human eyes.

### Key Insights
1. **No CI/CD exists** — there are zero GitHub Actions workflows. The harness is entirely local (justfile + scripts). An AI review sensor must work locally first.
2. **The pipeline gap is clear** — current flow is `planner → implementer → verifier → (manual) auditor`. The sensor should slot between verifier and human review as an automatic step.
3. **Existing computational sensors are strong** — StyleCop analyzers (532 warnings), .NET analyzers, xUnit tests (91). What's missing is the *inferential* layer that catches semantic/domain issues tests don't cover.
4. **The structured output format already exists** — agents output JSON `{task, result, files_changed, retries}`. The review sensor should follow this convention.

### Quick Stats
- **Existing sensors**: StyleCop (computational), .NET analyzers (computational), 91 xUnit tests (computational), auditor agent (inferential, manual)
- **Missing sensors**: Automated inferential review, mutation testing, architecture fitness checks
- **CI/CD**: None (no .github/workflows/)
- **Git hooks**: None (no pre-commit, husky, or hooks configured)
- **Agent definitions**: 4 existing (.github/agents/ — planner, implementer, verifier, auditor) + speckit agents

## How It Currently Works

### Current Agent Pipeline

```
User Request
    │
    ▼
┌──────────┐     ┌──────────────┐     ┌──────────┐     ┌─────────┐
│ Planner  │ ──▶ │ Implementer  │ ──▶ │ Verifier │ ──▶ │ Auditor │
│ (scope)  │     │ (code change)│     │(just val)│     │(domain) │
└──────────┘     └──────────────┘     └──────────┘     └─────────┘
                                            │                │
                                       AUTOMATIC         MANUAL ← GAP
                                       (always)         (domain only)
```

### Entry Points for Automation

| Trigger Point | Location | Current State | Sensor Opportunity |
|--------------|----------|---------------|-------------------|
| Post-validate | `scripts/harness.sh` line 44 | validate → domain_check → done | Insert review between validate+domain |
| Post-check | `justfile` line 50 (`check` recipe) | restore+build+test only | Add review recipe call |
| Post-commit | `.git/hooks/post-commit` | Does not exist | Could trigger review |
| PR creation | `.github/workflows/` | Does not exist | Future CI sensor |
| `just harness` | `justfile` line 470 | Delegates to `scripts/harness.sh` | Already the orchestrator |

### Existing Computational Sensors

| Sensor | Type | Config | Coverage |
|--------|------|--------|----------|
| StyleCop.Analyzers 1.1.118 | Linter | In .csproj | Style rules (532 warnings active) |
| .NET Analyzers (latest) | Static analysis | `EnableNETAnalyzers=true` | Code quality, null safety |
| EnforceCodeStyleInBuild | Build-time | `true` in .csproj | EditorConfig enforcement |
| xUnit tests (91) | Unit tests | CostSharingApp.Tests.csproj | Domain logic, algorithms |
| `just doctor` (7 checks) | Environment | justfile | SDK, emulator, NuGet health |

### What's Missing (Inferential Sensors)

| Gap | Fowler Category | Impact |
|-----|----------------|--------|
| Semantic code review | Inferential feedback | Catches logic errors tests miss |
| Domain invariant reasoning | Inferential feedback | Validates business rules beyond test assertions |
| Architecture drift detection | Inferential feedback | Catches boundary violations |
| PR-level summary | Inferential feedback | Reduces human review cognitive load |

## Architecture & Design

### Where the Sensor Should Live

**Option A: New justfile recipe `just review`**
- Fits existing pattern (all agent commands are justfile recipes)
- Can be called standalone or as part of `scripts/harness.sh`
- Uses `gh copilot` CLI or a custom script calling an LLM
- Produces structured output to `scratch/evidence/`

**Option B: New agent definition `.github/agents/reviewer.agent.md`**
- Follows existing agent pattern (planner, implementer, verifier, auditor)
- Provides instructions for AI-powered review
- Can be invoked by other agents in the pipeline

**Option C: Git hook (post-commit)**
- Runs automatically on every commit
- Lightweight — could just flag files for review
- Risk: slows down commits if review is slow

**Recommended: Option A + B combined**
- `reviewer.agent.md` defines the review agent's instructions and scope
- `just review` recipe invokes it (like `just harness` invokes scripts)
- `scripts/harness.sh` calls `just review` after validate passes
- Evidence output to `scratch/evidence/review-{timestamp}.md`

### Proposed Pipeline (After)

```
User Request
    │
    ▼
┌──────────┐     ┌──────────────┐     ┌──────────┐     ┌──────────┐     ┌─────────┐
│ Planner  │ ──▶ │ Implementer  │ ──▶ │ Verifier │ ──▶ │ Reviewer │ ──▶ │ Auditor │
│ (scope)  │     │ (code change)│     │(just val)│     │(AI review)│    │(domain) │
└──────────┘     └──────────────┘     └──────────┘     └──────────┘     └─────────┘
                                       AUTOMATIC        AUTOMATIC        CONDITIONAL
                                       (always)         (always)         (domain only)
```

### Review Scope (What the Sensor Should Check)

Based on Fowler's framework — inferential sensors should catch what computational sensors cannot:

1. **Logic errors** — "this condition seems inverted", "this loop may infinite"
2. **Domain violations** — "this split doesn't sum to total", "settlement creates phantom debt"
3. **Boundary violations** — "Core should not reference MAUI types"
4. **Pattern breaks** — "other services use DI, but this one uses `new`"
5. **Incomplete changes** — "you modified the service but not the test"
6. **Security concerns** — "this input isn't validated before use"

### What It Should NOT Check (Avoid Duplicating Computational Sensors)

- Style/formatting (StyleCop handles this)
- Null reference warnings (.NET analyzers handle this)
- Test failures (xUnit handles this)
- Build errors (dotnet build handles this)

## Integration Architecture

### Output Format (Following Existing Conventions)

```json
{
  "review": {
    "verdict": "approve | concern | reject",
    "findings": [
      {
        "severity": "critical | warning | info",
        "file": "path/to/file.cs",
        "line": 42,
        "category": "logic | domain | boundary | pattern | security",
        "message": "Human-readable finding",
        "suggestion": "How to fix (LLM-optimised for self-correction)"
      }
    ],
    "summary": "One-line review summary"
  }
}
```

### Trigger Mechanism

The simplest integration: modify `scripts/harness.sh` to add a review step:

```bash
# After validate passes, before final result
if run_validate; then
    if run_review; then       # ← NEW
        if domain_check; then
            RESULT="pass"
        fi
    fi
fi
```

### LLM-Optimised Output (Fowler's "Positive Prompt Injection")

Following Fowler's key insight, review findings should be written as instructions the implementing agent can act on:

> "❌ REVIEW: SplitCalculationService.cs:47 — The remainder is assigned to index 0, but the convention in this file is to assign to the LAST participant. Change `splits[0]` to `splits[^1]`."

Not just "found an issue" but "here's exactly what to do about it."

## Modification Considerations

### ✅ Safe to Modify
1. **`scripts/harness.sh`** — Add review step in the pipeline. Well-isolated, no other consumers.
2. **`.github/agents/`** — Add new agent definition. Follows existing pattern.
3. **`justfile`** — Add `review` recipe. Self-contained, doesn't affect other recipes.

### ⚠️ Modify with Caution
1. **`AGENTS.md`** — Update Multi-Agent Flow section to include reviewer. Other agents reference this.
2. **`docs/project-rules/harness.md`** — Update execution order. Defines the contract for all agents.

### Extension Points
1. **`scripts/harness.sh`** — clear insertion point at line 44 (after validate, before domain_check)
2. **`justfile`** — add recipe after `harness` recipe (line 470+)
3. **`.github/agents/`** — add `reviewer.agent.md` following verifier.agent.md pattern

## Prior Learnings

> No prior learnings found directly related to AI code review sensors.
> Scanned 3 plan folders (001-harness-l3, 002-harness-l4, 003-web-version).
> Existing discovery topics: emulator lifecycle, self-healing patterns, platform reduction.

### 📚 Relevant Insight from L4 Implementation
**Source**: L4 harness implementation (session history)
**Type**: decision
**What**: Self-healing uses `set +e` scoping to avoid `set -euo pipefail` killing the script on non-zero exits from probe commands.
**Relevance**: The review sensor will likely call an LLM (non-deterministic, may fail). It should use the same `set +e` pattern for resilience.
**Action**: Wrap review invocation in `set +e` / `set -e` scope with graceful fallback.

## Domain Context

> No domain registry found.

| Proposed Domain | Evidence | Boundary | Relevance |
|----------------|----------|----------|-----------|
| harness-infra | justfile, scripts/harness.sh, AGENTS.md, harness.md | Build/test/observe automation | Primary — the review sensor lives here |
| agent-pipeline | .github/agents/*.agent.md, harness.md Multi-Agent section | Agent orchestration and output contracts | Secondary — reviewer is a new agent in the pipeline |

## Critical Discoveries

### 🚨 Critical Finding 01: No CI/CD Exists
**Impact**: Critical
**What**: There are zero GitHub Actions workflows. All automation is local via justfile.
**Why It Matters**: The review sensor must work locally first. A CI-based approach (like a PR review bot) would require building CI from scratch — much larger scope.
**Required Action**: Design for local-first. CI integration can be Phase 2.

### 🚨 Critical Finding 02: Auditor Is Manual-Only
**Impact**: High
**What**: The auditor agent (`.github/agents/auditor.agent.md`) is only invoked when explicitly requested for domain logic changes. It never runs automatically.
**Why It Matters**: Domain invariant checking is the highest-value review. Making the reviewer automatically invoke domain checks (or incorporate the auditor's checklist) would close the biggest gap.
**Required Action**: The review sensor should include a lightweight domain invariant check (not full auditor, but a quick pass).

### 🚨 Critical Finding 03: Structured Exit Codes Enable Clean Integration
**Impact**: High (positive)
**What**: The harness already uses exit codes 0/1/2 (success/fail/self-healed). All recipes follow this convention.
**Why It Matters**: The review sensor can fit seamlessly — exit 0 if review passes, exit 1 if critical findings, exit 2 if findings were auto-correctable.

## External Research Opportunities

### Research Opportunity 1: Local LLM-Powered Code Review Tools

**Why Needed**: The review sensor needs to invoke an LLM. Options: `gh copilot` CLI, local model via Ollama, or API call. Need to know what's practical for local-first usage.
**Impact on Plan**: Determines whether the sensor can run without internet/API keys.
**Source Findings**: IA-01, DC-01

**Ready-to-use prompt:**
```
/deepresearch "Local LLM-powered code review for .NET projects.
Context: .NET 9 MAUI app, want to run AI code review locally as part of a justfile recipe.
Questions: 1) Can `gh copilot` CLI perform code review on staged changes?
2) What local models (Ollama, llama.cpp) are suitable for code review?
3) Best practices for diff-based review (feed only changed lines vs full file)?
4) How to format review output as structured JSON?
5) Token limits and strategies for large changesets?"
```

### Research Opportunity 2: GitHub Actions for .NET MAUI + AI Review

**Why Needed**: Phase 2 would add CI. Need to understand GitHub Actions patterns for .NET MAUI builds + automated review.
**Impact on Plan**: Informs whether CI review runs on PR creation or push.

**Ready-to-use prompt:**
```
/deepresearch "GitHub Actions CI for .NET MAUI Android + AI code review.
Context: .NET 9 MAUI Android-only app, justfile-based local harness, want to add CI.
Questions: 1) Minimal workflow for build+test of .NET MAUI Android?
2) How to run AI-powered code review in CI (GitHub Copilot, custom action)?
3) How to post review findings as PR comments?
4) Cost/performance of running .NET MAUI builds on GitHub runners?"
```

## Recommendations

### If Implementing This Feature
1. **Start local** — `just review` recipe that reviews staged/committed changes
2. **Use git diff** — feed only changed files to the review, not the entire codebase
3. **Follow existing patterns** — agent definition + justfile recipe + harness.sh integration
4. **Structured output** — JSON findings to `scratch/evidence/review-*.json`
5. **Non-blocking initially** — exit 0 even with findings (warn, don't fail). Upgrade to blocking after confidence builds.

### Suggested Implementation Phases
1. **Phase 1**: `reviewer.agent.md` definition + `just review` recipe (manual invocation)
2. **Phase 2**: Integrate into `scripts/harness.sh` (automatic after validate)
3. **Phase 3**: Add GitHub Actions workflow with review on PR (CI sensor)

## Appendix: File Inventory

### Files to Create
| File | Purpose |
|------|---------|
| `.github/agents/reviewer.agent.md` | Agent definition with review scope and output format |
| `scripts/review.sh` | Review script (invokes diff analysis + LLM review) |

### Files to Modify
| File | Change |
|------|--------|
| `justfile` | Add `review` recipe |
| `scripts/harness.sh` | Add `run_review()` step in pipeline |
| `AGENTS.md` | Update Multi-Agent Flow to include reviewer |
| `docs/project-rules/harness.md` | Update execution order |

### Existing Files (Reference)
| File | Relevance |
|------|-----------|
| `.github/agents/verifier.agent.md` | Pattern to follow for reviewer |
| `.github/agents/auditor.agent.md` | Domain checks to incorporate |
| `justfile` (line 470) | Insertion point for review recipe |
| `scripts/harness.sh` (line 44) | Insertion point for review step |

## Next Steps

1. Run `/deepresearch` for local LLM review tooling options
2. Or skip research and run `/plan-1b-specify "AI code review sensor"` to create the spec
3. Key decisions needed:
   - LLM invocation method (gh copilot CLI vs API vs local model)
   - Blocking vs non-blocking (fail the pipeline or just warn?)
   - Scope: all changes or only domain-related files?

---

**Research Complete**: 2026-05-13T11:35:00+10:00
**Report Location**: docs/plans/004-ai-code-review-sensor/research-dossier.md
