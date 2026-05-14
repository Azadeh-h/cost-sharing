# Add Mutation Testing — Implementation Plan

**Mode**: Simple
**Plan Version**: 1.0.0
**Created**: 2026-05-13
**Spec**: `docs/plans/005-mutation-testing/mutation-testing-spec.md`
**Status**: COMPLETE

## Summary

The cost-sharing app needs mutation testing to measure whether its 75 xUnit tests actually catch real bugs in financial calculation code. Stryker.NET will be installed as a local dotnet tool, configured to mutate only `CostSharing.Core` (pure `net9.0`, no MAUI deps), and integrated into the developer workflow via `just mutate`. The initial mutation score baseline will be recorded and documented in AGENTS.md and harness.md.

## Target Domains

| Domain | Status | Relationship | Role |
|--------|--------|-------------|------|
| testing-infrastructure | **NEW** | **create** | Stryker config, justfile recipe, documentation |
| algorithms | existing (informal) | **consume** | Mutation target (DebtSimplificationAlgorithm) |
| calculation-services | existing (informal) | **consume** | Mutation target (SplitCalculation, DebtCalculation) |

## Harness Strategy

- **Current Maturity**: L4 (self-healing)
- **Target Maturity**: L4 (no change — add `just mutate` to existing harness)
- **Boot Command**: `just validate`
- **Health Check**: `just test`
- **Interaction Model**: Terminal (dotnet CLI + justfile)
- **Evidence Capture**: Terminal output (mutation score, surviving mutants count)
- **Pre-Phase Validation**: Run `just validate` before starting

## Domain Manifest

| File | Domain | Classification | Rationale |
|------|--------|---------------|-----------|
| `CostSharingApp/.config/dotnet-tools.json` | testing-infrastructure | internal | New — dotnet tool manifest for Stryker |
| `CostSharingApp/stryker-config.json` | testing-infrastructure | contract | New — mutation testing configuration |
| `justfile` | testing-infrastructure | contract | Modify — add `just mutate` recipe |
| `.gitignore` | testing-infrastructure | internal | Modify — add `StrykerOutput/` |
| `AGENTS.md` | testing-infrastructure | contract | Modify — add `just mutate` to Key Commands |
| `docs/project-rules/harness.md` | testing-infrastructure | contract | Modify — add mutation testing to Interact + quality gates |

## Key Findings

| # | Impact | Finding | Action |
|---|--------|---------|--------|
| 01 | High | No dotnet tool manifest exists — `.config/dotnet-tools.json` not found | Create manifest with `dotnet new tool-manifest` before installing Stryker (T001) |
| 02 | High | `.gitignore` does not ignore `StrykerOutput/` | Add entry in T005 |
| 03 | Medium | Justfile uses comment headers + recipe pattern for commands | Follow same pattern for `just mutate` (T004) |
| 04 | Medium | AGENTS.md Key Commands at lines 44-59; harness.md Interact at lines 22-40 | Add entries following existing format (T006, T007) |
| 05 | Low | No build conflicts — Core csproj is plain `net9.0`, test csproj has `IsPackable=false`, no Directory.Build.props | Stryker should work without issues |
| 06 | Low | Stryker can target test project directly (no solution file needed) | Use `--test-project` flag pointing to test csproj |

## Implementation

**Objective**: Install Stryker.NET, configure it to mutate `CostSharing.Core`, integrate into justfile, and document.
**Testing Approach**: Lightweight — verify Stryker installs, runs, and produces a report with score ≥ 60%.

### Tasks

| Status | ID | Task | Domain | Path(s) | Done When | Notes |
|--------|-----|------|--------|---------|-----------|-------|
| [x] | T001 | Create dotnet tool manifest and install Stryker.NET | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/CostSharingApp/.config/dotnet-tools.json` | `dotnet tool list` shows `dotnet-stryker` installed | Run `cd CostSharingApp && dotnet new tool-manifest && dotnet tool install dotnet-stryker` |
| [x] | T002 | Create stryker-config.json | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/CostSharingApp/stryker-config.json` | Config file exists with correct project/test-project paths, thresholds (break=60, low=70, high=80), and mutate glob targeting `CostSharing.Core` only | Exclude `Models/` folder from mutation; use HTML + progress reporters |
| [x] | T003 | Run initial Stryker baseline | testing-infrastructure | — | Stryker completes successfully, produces HTML report, mutation score is recorded | Run from `CostSharingApp/`; expect score ~60-75%; record actual score in T007 notes |
| [x] | T004 | Add `just mutate` recipe to justfile | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just mutate` executes Stryker and exits with appropriate code (0 if score ≥ break threshold) | Follow existing recipe pattern with comment header; `cd CostSharingApp && dotnet stryker` |
| [x] | T005 | Add `StrykerOutput/` to .gitignore | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/.gitignore` | `StrykerOutput/` is listed in .gitignore | Per finding 02 |
| [x] | T006 | Update AGENTS.md with `just mutate` command | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/AGENTS.md` | Key Commands table includes `just mutate` row with description; mutation testing section documents expected score and interpretation | Per finding 04; follow existing table format at lines 44-59 |
| [x] | T007 | Update harness.md with mutation testing | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/docs/project-rules/harness.md` | Interact section lists `just mutate`; quality gates mention mutation score baseline | Per finding 04; add bullet under Endpoints/Commands at lines 22-40 |
| [x] | T008 | Verify `just validate` still passes | testing-infrastructure | — | `just validate` exits 0; all 88 tests pass; no production code was changed | Final sanity check — no regressions |

### Acceptance Criteria

- [x] AC1: Stryker.NET is installed as a local dotnet tool — `dotnet stryker --version` works from `CostSharingApp/`
- [x] AC2: Configuration targets `CostSharing.Core` only — MAUI project excluded
- [x] AC3: HTML mutation report is generated showing score, surviving/killed mutants per file
- [x] AC4: `just mutate` command exists and runs Stryker end-to-end
- [x] AC5: Initial mutation score baseline is documented (64.34%)
- [x] AC6: Thresholds configured: break=60, low=70, high=80
- [x] AC7: AGENTS.md documents `just mutate`, expected score, interpretation guidance
- [x] AC8: harness.md lists mutation testing in Interact section
- [x] AC9: `just validate` continues to pass (91 tests, no production code changes)
- [x] AC10: `StrykerOutput/` is gitignored

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Stryker.NET doesn't support .NET 9 | Low | High | Check latest version on install; fallback to specifying net8.0 target |
| Initial mutation score < 50% | Medium | Low | Expected and valuable; document as baseline, not failure |
| Mutation run is slow (> 5 min) | Medium | Medium | Config scopes to Tier 1 targets; enable concurrent test runners |
| Stryker conflicts with MAUI project in solution | Low | Medium | Config explicitly excludes MAUI project; point Stryker at test csproj only |
