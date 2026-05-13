# Continuous Drift Detection — Implementation Plan

**Mode**: Simple
**Plan Version**: 1.0.0
**Created**: 2026-05-13
**Spec**: `docs/plans/006-drift-detection/drift-detection-spec.md`
**Status**: COMPLETE

## Summary

Add a `just drift` command that runs 4 sensors — dependency scanning, coverage quality, dead code (interfaces), and doc drift — and streams a structured JSON report to stdout. Each sensor detects gradual codebase degradation that no single commit introduces. No production code changes; purely scripts and justfile wiring.

## Target Domains

| Domain | Status | Relationship | Role |
|--------|--------|-------------|------|
| testing-infrastructure | existing (informal) | **modify** | Drift script, justfile recipe, docs |
| algorithms | existing (informal) | **consume** | Scanned by coverage + dead code sensors |
| calculation-services | existing (informal) | **consume** | Scanned by coverage + dead code sensors |

## Harness Strategy

- **Current Maturity**: L4 (self-healing)
- **Target Maturity**: L4 (no change — add `just drift`)
- **Boot Command**: `just validate`
- **Health Check**: `just test`
- **Interaction Model**: Terminal
- **Evidence Capture**: Terminal output (JSON to stdout)
- **Pre-Phase Validation**: Run `just validate` before starting

## Domain Manifest

| File | Domain | Classification | Rationale |
|------|--------|---------------|-----------|
| `scripts/drift-scan.sh` | testing-infrastructure | internal | New — unified drift detection script with 4 sensors |
| `justfile` | testing-infrastructure | contract | Modify — add `just drift` recipe |
| `AGENTS.md` | testing-infrastructure | contract | Modify — add `just drift` to Key Commands + Drift Detection section |
| `docs/project-rules/harness.md` | testing-infrastructure | contract | Modify — add drift detection to Interact + History |

## Key Findings

| # | Impact | Finding | Action |
|---|--------|---------|--------|
| 01 | High | DI-registered interfaces will false-positive as "dead code" — `IDriveAuthService`, `IDriveSyncService`, `IOfflineQueueService`, `IConflictResolver` are resolved at runtime via DI in `MauiProgram.cs` | Add exclusion list for DI-registered interfaces; only flag interfaces with zero references anywhere (tests OR app) |
| 02 | High | Dependency scan requires network — `dotnet list package --outdated` queries NuGet feeds | Detect offline gracefully; report "skipped" instead of crashing |
| 03 | Medium | AGENTS.md has specific driftable facts: test count ("88 tests"), mutation baseline ("64.34%"), command count | Parse with targeted regex; compare against runtime values |
| 04 | Medium | harness.md lists 15 commands in Interact but AGENTS.md says "16 named commands" — existing mismatch | Doc drift sensor should catch this on first run |
| 05 | Medium | `coverlet.collector` is sufficient for `dotnet test --collect:"XPlat Code Coverage"` — no additional packages needed | Use collector-based CLI path for coverage sensor |
| 06 | Low | Coverage collection adds overhead (~2-5s) | Acceptable for on-demand `just drift`; not in the hot validate path |

## Implementation

**Objective**: Create `scripts/drift-scan.sh` with 4 sensors and wire into justfile as `just drift`.
**Testing Approach**: Lightweight — verify each sensor runs, produces output, and `just validate` still passes.

### Tasks

| Status | ID | Task | Domain | Path(s) | Done When | Notes |
|--------|-----|------|--------|---------|-----------|-------|
| [x] | T001 | Create dependency scanner sensor | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/scripts/drift-scan.sh` | Runs `dotnet list package --outdated` and `--vulnerable`; outputs JSON with package name, current version, latest version, severity | Handles offline gracefully (per finding 02); strips ANSI codes |
| [x] | T002 | Add coverage quality sensor | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/scripts/drift-scan.sh` | Runs `dotnet test --collect:"XPlat Code Coverage"`; parses cobertura XML; outputs per-file line coverage for CostSharing.Core | Uses existing coverlet.collector (per finding 05); filenames are relative to Core source |
| [x] | T003 | Add dead code sensor (interfaces) | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/scripts/drift-scan.sh` | Scans `CostSharing.Core/Interfaces/*.cs`; for each interface, checks references in both test and app directories; reports unreferenced interfaces | Exclude DI-registered interfaces via exclusion list (per finding 01); all 6 current interfaces are DI-registered |
| [x] | T004 | Add doc drift sensor | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/scripts/drift-scan.sh` | Checks AGENTS.md test count vs actual `dotnet test` output; checks justfile command count vs harness.md documentation | Detected 3 existing drifts: test count 88→91, command count 16→19 |
| [x] | T005 | Add unified report and health summary | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/scripts/drift-scan.sh` | Script outputs JSON with per-sensor status (✅/⚠️/❌), finding counts, and overall health verdict | Progress to stderr, JSON to stdout |
| [x] | T006 | Add `just drift` recipe to justfile | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/justfile` | `just drift` runs all 4 sensors and produces structured report | Follow existing recipe pattern |
| [x] | T007 | Update AGENTS.md | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/AGENTS.md` | Key Commands includes `just drift`; Drift Detection section documents sensors and interpretation | Added full Drift Detection section |
| [x] | T008 | Update harness.md | testing-infrastructure | `/Users/azadehhassanzadeh/Source/cost-sharing/docs/project-rules/harness.md` | Interact section lists `just drift`; History entry added | Added history row with baseline metrics |
| [x] | T009 | Verify `just validate` still passes | testing-infrastructure | — | `just validate` exits 0; all tests pass; no production code changed | 91 tests pass, exit 0 |

### Acceptance Criteria

- [x] AC1: `just drift` command exists and runs all 4 sensors
- [x] AC2: Dependency scanner reports outdated packages with version info
- [x] AC3: Coverage sensor produces per-file line coverage for CostSharing.Core
- [x] AC4: Dead code sensor identifies unreferenced interfaces (with DI exclusion list)
- [x] AC5: Doc drift sensor checks test count in AGENTS.md vs actual
- [x] AC6: Report is structured JSON streamed to stdout
- [x] AC7: Each sensor outputs health status (✅ healthy / ⚠️ drifting / ❌ critical)
- [x] AC8: `just validate` continues to pass
- [x] AC9: AGENTS.md documents `just drift`
- [x] AC10: harness.md updated with drift detection

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Dead code false positives from DI | High | Medium | Exclusion list for DI-registered interfaces |
| Dependency scan fails offline | Medium | Low | Graceful degradation — report "skipped" |
| Doc drift regex breaks on format changes | Medium | Medium | Keep patterns simple; target specific numeric facts only |
| Coverage collection slow | Low | Low | Acceptable for on-demand scan (~5-10s overhead) |
