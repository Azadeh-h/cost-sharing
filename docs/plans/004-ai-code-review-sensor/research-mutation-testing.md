# Research Report: Add Mutation Testing

**Generated**: 2026-05-13T12:30:00+10:00
**Research Query**: "add Mutation testing"
**Mode**: Plan-Associated (004-ai-code-review-sensor)
**Location**: docs/plans/004-ai-code-review-sensor/research-mutation-testing.md
**FlowSpace**: Not Available
**Findings**: 68 total across 8 subagents

## Executive Summary

### What It Does
Mutation testing systematically introduces small code changes (mutants) into production code and verifies that the existing test suite detects them. It measures test suite *effectiveness*, not just coverage.

### Business Purpose
The cost-sharing app handles financial calculations (splits, debts, settlements) where correctness is critical. Mutation testing would validate that the 88 xUnit tests actually catch real bugs — not just achieve line coverage. This is identified as a missing "inferential sensor" in the AI code review sensor plan.

### Key Insights
1. **No mutation testing exists** — no Stryker.NET config, no mutation reports, no prior attempts
2. **Ideal target exists**: `CostSharing.Core` is a pure `net9.0` library (no MAUI dependency), testable in isolation with 3 key mutation targets
3. **Test assertions have gaps** — several tests use broad assertions (`Count <= 2`, `total <= original`) that would let mutants survive, revealing real quality improvement opportunities

### Quick Stats
- **Components**: 3 core mutation targets (SplitCalculation, DebtCalculation, DebtSimplification)
- **Dependencies**: xUnit 2.9.2, Moq 4.20.72 (unused), coverlet.collector 6.0.2
- **Test Coverage**: 75 test methods across 6 classes; no coverage thresholds set
- **Complexity**: Low-Medium (pure arithmetic/algorithmic code, well-isolated core library)
- **Prior Learnings**: 0 direct mutation testing learnings; mutation testing flagged as missing sensor in plan 004
- **Domains**: No formal domain system; 3 natural domains identified (Algorithms, Services, Models)

## How It Currently Works

### Entry Points
Testing is invoked through the justfile or direct dotnet CLI:

| Entry Point | Type | Location | Purpose |
|------------|------|----------|---------|
| `just validate` | CLI | justfile:49-134 | Build + test with self-healing |
| `just test` | CLI | justfile:24 | Run xUnit tests |
| `just test-log` | CLI | justfile:25-40 | Run tests with TRX evidence |
| `dotnet test` | CLI | Direct | Raw test execution |

### Core Test-Source Mapping

| Test Class | Source Target | Test Count |
|-----------|-------------|------------|
| `SplitCalculationServiceTests` | `SplitCalculationService.cs` | 14 |
| `DebtCalculationServiceTests` | `DebtCalculationService.cs` | 11 |
| `DebtSimplificationAlgorithmTests` | `DebtSimplificationAlgorithm.cs` | 10 |
| `InvitationLinkingServiceTests` | Models + invitation logic | 16 |
| `GmailInvitationServiceTests` | Gmail invitation contracts | 16 |
| `CustomSplitViewModelTests` | `SplitCalculationService.cs` | 8 |

### Test Project Structure
```
tests/CostSharingApp.Tests/          (net9.0, xUnit)
├── Services/
│   ├── SplitCalculationServiceTests.cs
│   ├── DebtCalculationServiceTests.cs
│   ├── InvitationLinkingServiceTests.cs
│   └── GmailInvitationServiceTests.cs
├── Algorithms/
│   └── DebtSimplificationAlgorithmTests.cs
└── ViewModels/
    └── CustomSplitViewModelTests.cs
```

## Architecture & Design

### Component Map — Mutation Testing Scope

#### Tier 1: Primary Mutation Targets (highest ROI)
- **`SplitCalculationService`** (`src/CostSharing.Core/Services/SplitCalculationService.cs:12-142`)
  - Deterministic arithmetic, good exact assertions
  - Methods: `CalculateEvenSplit`, `CalculateCustomSplit`
  - Mutation hotspots: decimal rounding, remainder distribution, ordering logic

- **`DebtCalculationService`** (`src/CostSharing.Core/Services/DebtCalculationService.cs:12-232`)
  - High business value — encodes who owes whom
  - Methods: `CalculateDebts` (2 overloads)
  - Mutation hotspots: balance aggregation, settlement subtraction, zero-threshold, `DateTime.UtcNow`/`Guid.NewGuid()` inline usage

- **`DebtSimplificationAlgorithm`** (`src/CostSharing.Core/Algorithms/DebtSimplificationAlgorithm.cs:9-185`)
  - Core Min-Cash-Flow algorithm, self-contained (no DI, no I/O)
  - Methods: `SimplifyDebts`, `CalculateNetBalances`, `GreedyMatching`, `GetSimplificationSummary`
  - Mutation hotspots: greedy creditor/debtor matching, `Math.Abs >= 0.01m` threshold, `OrderBy` direction

#### Tier 2: Secondary Targets (after Tier 1 achieves good scores)
- **`InvitationLinkingService`** — email normalization, invitation logic
- **`GmailInvitationService`** — invitation contracts
- **Domain Models** — property validations, state transitions

#### Out of Scope for Mutation Testing
- `src/CostSharingApp/` (MAUI app) — no test project reference, platform-specific
- `Views/`, `Platforms/Android/`, `Resources/` — UI layer, restricted area

### Design Patterns Identified (PS findings)
1. **AAA Structure**: Consistent Arrange/Act/Assert across all test classes
2. **Feature-based organization**: Tests mirror source folder structure
3. **Pure unit tests**: No mocking despite Moq being referenced
4. **Hand-built test data**: Inline object initializers, no builders/factories
5. **xUnit-only assertions**: `Assert.Equal`, `Assert.Single`, `Assert.Contains`, `Assert.Throws`

## Dependencies & Integration

### Test Infrastructure Dependencies

| Package | Version | Purpose | Mutation Testing Impact |
|---------|---------|---------|------------------------|
| `xunit` | 2.9.2 | Test framework | Stryker supports fully |
| `xunit.runner.visualstudio` | 2.8.2 | Test runner | Compatible |
| `Microsoft.NET.Test.Sdk` | 17.12.0 | Test SDK | Required by Stryker |
| `coverlet.collector` | 6.0.2 | Coverage collection | Complementary to mutation |
| `Moq` | 4.20.72 | Mocking (unused) | N/A |

### Project Reference Chain
```
CostSharingApp.Tests (net9.0)
    └── CostSharing.Core (net9.0)
            └── sqlite-net-pcl 1.9.172

CostSharingApp (net9.0-android)
    └── CostSharing.Core (net9.0)
```

**Key insight**: Test project → Core only. No reference to MAUI app project. Stryker can target `CostSharing.Core` cleanly.

### Solution-Wide Config
- No `global.json`, `Directory.Build.props`, `Directory.Packages.props`, or `NuGet.config` found
- Code quality: `.editorconfig` + `EnforceCodeStyleInBuild` + `EnableNETAnalyzers` + StyleCop in app project

## Quality & Testing

### Current Test Quality Assessment

#### Strengths (mutation-ready)
- Tests assert exact decimal amounts for split calculations
- Tests verify specific exception types and messages
- Algorithm tests check conservation of total amounts
- Good boundary testing (0 members, 1 member, negative amounts)

#### Weaknesses (likely to let mutants survive)
- **Broad assertions**: `Assert.True(result.Count <= 2)` — any count ≤ 2 passes
- **Total-only checks**: `Assert.True(totalAmount <= originalTotal)` — doesn't verify per-transaction correctness
- **Missing pairing assertions**: Debt tests check creditor totals but not specific debtor→creditor pairs
- **Copied helper logic**: `InvitationLinkingServiceTests` and `GmailInvitationServiceTests` use private helper copies instead of production code
- **Non-deterministic inputs**: `DateTime.UtcNow` and `Guid.NewGuid()` used inline in `DebtCalculationService`

### Untested Core Services (major gap)
The following `CostSharing.Core/Services/` have **no dedicated tests**:
- `ExpenseService.cs`, `GroupService.cs`, `SettlementService.cs`
- `AuthService.cs`, `DriveAuthService.cs`
- `ConflictResolutionService.cs`, `OfflineQueueService.cs`
- `CacheService.cs`, `ConfigurationService.cs`
- `LoggingService.cs`, `ErrorService.cs`, `SessionService.cs`
- `DatabaseMigrations.cs`, `DriveErrorHandler.cs`, `DriveSyncService.cs`

### Current Quality Gates
- Only gate: `dotnet test` passes (exit code 0)
- No coverage thresholds
- No mutation score baselines
- No CI/CD pipeline

## Modification Considerations

### ✅ Safe to Add Mutation Testing
1. **`CostSharing.Core` assembly** — pure `net9.0`, no MAUI deps, clean project reference from tests
2. **Stryker.NET as dotnet tool** — installs via `dotnet tool`, no project file changes needed
3. **justfile integration** — add `just mutate` command alongside existing `just test`

### ⚠️ Modify with Caution
1. **Test assertions** — tightening assertions to kill more mutants is high-value but must not break existing green tests
2. **DebtCalculationService** — `DateTime.UtcNow`/`Guid.NewGuid()` inline usage makes some mutations hard to detect deterministically

### 🚫 Danger Zones
1. **Do NOT mutate `CostSharingApp` (MAUI project)** — no test coverage, platform-specific
2. **Do NOT add Stryker as project reference** — it's a dotnet tool, not a package reference
3. **Avoid mutating model-only code** — property getters/setters generate noise, low signal

### Extension Points
1. **Stryker config file** (`stryker-config.json`) — configure mutation scope, thresholds, reporters
2. **justfile** — add `just mutate` recipe
3. **AGENTS.md** — document mutation testing command and expected scores

## Prior Learnings (From Previous Implementations)

### 📚 Prior Learning PL-09: Mutation Testing Identified as Missing Sensor
**Source**: docs/plans/004-ai-code-review-sensor/research-dossier.md:24-27
**Original Type**: insight
**Finding**: Prior research explicitly calls out "mutation testing" as a missing inferential sensor in the AI code review sensor plan.
**Why This Matters Now**: This research directly addresses that identified gap.
**Action**: Implement Stryker.NET as the mutation testing sensor.

### 📚 Prior Learning PL-11: Property-Based Testing Recommended for Debt Simplification
**Source**: specs/001-cost-sharing-app/research.md:229-233
**Original Type**: insight
**Finding**: Prior research recommends property-based testing for debt simplification correctness.
**Why This Matters Now**: Property-based tests would complement mutation testing by generating diverse inputs that catch more mutants.
**Action**: Consider adding property-based tests (FsCheck) alongside mutation testing for the DebtSimplification algorithm.

### Prior Learnings Summary

| ID | Type | Source Plan | Key Insight | Action |
|----|------|-------------|-------------|--------|
| PL-09 | insight | 004-ai-code-review-sensor | Mutation testing flagged as missing sensor | Implement Stryker.NET |
| PL-11 | insight | specs/research | Property-based testing recommended for debts | Consider FsCheck complement |

## Domain Context

> No domain registry found. Consider running `/plan-v2-extract-domain` to formalize domain boundaries.

### Natural Domains Identified

| Proposed Domain | Evidence | Boundary | Mutation Testing Value |
|----------------|----------|----------|----------------------|
| `algorithms` | `CostSharing.Core/Algorithms/` | Self-contained, no DI/IO | ⭐ Highest — pure logic, perfect isolation |
| `calculation-services` | `CostSharing.Core/Services/Split*,Debt*` | Minimal deps, pure math | ⭐ High — critical business logic |
| `invitation` | `CostSharing.Core/Services/Invitation*,Gmail*` | Some I/O coupling | Medium — email normalization logic |
| `models` | `CostSharing.Core/Models/` | Data structures, enums | Low — mostly getters/setters |

### Mutation Testing Should Respect Domain Seams
- Mutate `Algorithms/` → run only algorithm tests
- Mutate `Services/Split*,Debt*` → run only service tests
- This keeps mutation runs fast and results actionable

## Critical Discoveries

### 🚨 Critical Finding 01: Test Assertions Will Let Mutants Survive
**Impact**: Critical
**Source**: QT-03, QT-04, QT-08, PS-10
**What**: Several tests use broad assertions (`Count <= 2`, `total <= original`) rather than exact values. The DebtSimplification tests especially check properties (count, total) but not the exact transaction graph.
**Why It Matters**: Initial mutation score will likely be lower than expected. This is actually *the point* — mutation testing will reveal exactly where tests need strengthening.
**Required Action**: After initial Stryker run, use surviving mutants as a roadmap for assertion tightening.

### 🚨 Critical Finding 02: CostSharing.Core Is the Perfect Mutation Target
**Impact**: Critical (positive)
**Source**: IA-01, IA-04, DC-05, DB-05
**What**: `CostSharing.Core` is a pure `net9.0` class library with zero MAUI dependencies. The test project references only Core. Stryker can mutate Core and run tests without any MAUI/Android SDK involvement.
**Why It Matters**: This eliminates the biggest risk (MAUI compatibility with Stryker). Mutation testing can be added with minimal friction.
**Required Action**: Configure Stryker to target `CostSharing.Core` only.

### 🚨 Critical Finding 03: No CI/CD Pipeline Exists
**Impact**: High
**Source**: IA-08, DE-08
**What**: No GitHub Actions workflows or CI pipeline exists. Mutation testing will be local-only initially.
**Why It Matters**: Mutation score regressions won't be caught automatically. Consider adding mutation testing to a future CI pipeline.
**Required Action**: Add `just mutate` to justfile for local use. Plan CI integration later.

### 🚨 Critical Finding 04: Copied Helper Logic in Invitation Tests
**Impact**: High
**Source**: QT-06
**What**: `InvitationLinkingServiceTests` and `GmailInvitationServiceTests` use private helper copies of production logic. Mutations in production code may not be detected.
**Why It Matters**: These tests give false confidence — they test their own copies, not the real code.
**Required Action**: Exclude invitation services from initial mutation scope, or fix tests to use production code first.

## Recommendations

### If Adding Mutation Testing (recommended approach)

1. **Install Stryker.NET as a local dotnet tool**
   ```bash
   cd CostSharingApp
   dotnet new tool-manifest  # if .config/dotnet-tools.json doesn't exist
   dotnet tool install dotnet-stryker
   ```

2. **Create `stryker-config.json`** in `CostSharingApp/` with:
   - Project: `CostSharing.Core`
   - Test project: `CostSharingApp.Tests`
   - Mutate only `src/CostSharing.Core/**/*.cs`
   - Exclude: `Models/` (low signal), platform code
   - Reporters: html, dashboard, progress
   - Thresholds: start with break=60, low=70, high=80

3. **Add `just mutate` to justfile**

4. **Run initial baseline** — expect mutation score ~60-75% due to weak assertions

5. **Use surviving mutants** to systematically tighten assertions

6. **Update AGENTS.md** with mutation testing command and expected score

### Phased Approach

| Phase | Scope | Goal |
|-------|-------|------|
| 1 | `DebtSimplificationAlgorithm` only | Prove Stryker works, establish baseline |
| 2 | Add `SplitCalculationService` + `DebtCalculationService` | Core financial logic |
| 3 | Tighten assertions based on surviving mutants | Improve mutation score to 80%+ |
| 4 | Add to justfile + AGENTS.md | Operationalize |

## External Research Opportunities

### Research Opportunity 1: Stryker.NET Configuration for .NET 9

**Why Needed**: Stryker.NET compatibility with `net9.0` target framework needs confirmation. The codebase uses .NET 9 SDK.
**Impact on Plan**: Blocking — if Stryker doesn't support net9.0, an alternative framework is needed.
**Source Findings**: IA-10, DC-01

**Ready-to-use prompt:**
```
/deepresearch "Stryker.NET mutation testing framework compatibility with .NET 9 (net9.0).
Specifically: 
1. Does Stryker.NET support net9.0 target framework?
2. What is the latest Stryker.NET version and its .NET compatibility matrix?
3. Are there known issues with Stryker.NET and xUnit 2.9.x?
4. Best practices for configuring Stryker with coverlet.collector?
5. Performance optimization for small test suites (~75 tests, decimal-heavy arithmetic)?
6. Recommended stryker-config.json settings for a class library project?"
```

### Research Opportunity 2: Mutation Testing Best Practices for Financial Code

**Why Needed**: Financial calculation code has specific mutation testing concerns (decimal precision, rounding modes, boundary conditions).
**Impact on Plan**: Medium — affects which mutators to enable/disable.
**Source Findings**: IA-05, QT-07

**Ready-to-use prompt:**
```
/deepresearch "Mutation testing best practices for financial/decimal calculation code in C#/.NET.
Specifically:
1. Which Stryker.NET mutators are most valuable for decimal arithmetic code?
2. Should 'arithmetic operator replacement' be prioritized over 'equality operator replacement'?
3. How to handle non-deterministic code (DateTime.UtcNow, Guid.NewGuid) in mutation testing?
4. Recommended mutation score thresholds for financial software?
5. How to interpret surviving mutants in rounding/precision-sensitive code?"
```

---

**After External Research:**
- To conduct external research: Run the `/deepresearch` commands above
- To skip and proceed: Run `/plan-1b-specify "add mutation testing to cost-sharing app"` (unresolved opportunities will be noted as a soft warning)

## Appendix: File Inventory

### Core Files (Mutation Targets)
| File | Purpose | Lines |
|------|---------|-------|
| `src/CostSharing.Core/Services/SplitCalculationService.cs` | Even/custom split math | 142 |
| `src/CostSharing.Core/Services/DebtCalculationService.cs` | Debt aggregation + settlements | 232 |
| `src/CostSharing.Core/Algorithms/DebtSimplificationAlgorithm.cs` | Min-Cash-Flow greedy matching | 185 |

### Test Files
| File | Purpose | Tests |
|------|---------|-------|
| `tests/.../Services/SplitCalculationServiceTests.cs` | Split calculation tests | 14 |
| `tests/.../Services/DebtCalculationServiceTests.cs` | Debt calculation tests | 11 |
| `tests/.../Algorithms/DebtSimplificationAlgorithmTests.cs` | Algorithm tests | 10 |
| `tests/.../Services/InvitationLinkingServiceTests.cs` | Invitation logic tests | 16 |
| `tests/.../Services/GmailInvitationServiceTests.cs` | Gmail invitation tests | 16 |
| `tests/.../ViewModels/CustomSplitViewModelTests.cs` | ViewModel tests | 8 |

### Configuration Files
| File | Purpose |
|------|---------|
| `CostSharingApp.Tests.csproj` | Test project config (xUnit, coverlet, Moq) |
| `CostSharing.Core.csproj` | Core library config (net9.0, sqlite-net-pcl) |
| `justfile` | Build/test/validate commands |
| `.editorconfig` | Code style rules |

## Harness Status

- **Maturity**: L4 (self-healing)
- **Boot**: `just validate` (~10-30s)
- **Observe**: stdout/stderr, TRX reports, screenshots
- **Mutation testing fit**: Excellent — `just mutate` would integrate naturally alongside `just test`

## Next Steps

1. **Optional**: Run `/deepresearch` prompts above for Stryker.NET compatibility confirmation
2. **Proceed**: Run `/plan-1b-specify "add mutation testing to cost-sharing app"` to create specification
3. **Or**: Ask follow-up questions about specific findings

---

**Research Complete**: 2026-05-13T12:30:00+10:00
**Report Location**: docs/plans/004-ai-code-review-sensor/research-mutation-testing.md
