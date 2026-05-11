# Harness L3: Emulator Boot + UI Testing

**Mode**: Simple

ℹ️ Consider running `/plan-1a-explore` for deeper codebase understanding

## Summary

Upgrade the agent harness from L2 (build + test automation) to **L3** (full interaction + evidence) by enabling agents to boot an Android emulator, launch the app, drive UI interactions, and capture screenshots as evidence. This gives agents a complete feedback loop: they can see whether their code changes actually work in the running app, not just whether the build passes.

## Goals

- **Visual verification** — Agents can see what the app looks like after code changes
- **UI regression detection** — Catch layout breaks, missing data, navigation failures that unit tests miss
- **Evidence capture** — Screenshot artifacts prove the app works, not just that it compiles
- **Automated app launch** — `just boot-emulator` + `just launch-app` with no manual steps
- **Smoke test suite** — ADB-scripted navigation through 2-3 main screens with screenshot evidence

## Non-Goals

- Full end-to-end test coverage of every screen and edge case
- Performance benchmarking or stress testing
- Multi-device testing (different screen sizes, API levels)
- Cloud-based device farm integration (Firebase Test Lab, etc.)
- Accessibility testing automation

## Target Domains

| Domain | Status | Relationship | Role in This Feature |
|--------|--------|-------------|---------------------|
| harness-infrastructure | existing (informal) | **modify** | Add emulator boot, app launch, screenshot capture, UI test recipes |

## Complexity

- **Score**: CS-2 (small)
- **Breakdown**: S=1, I=2, D=0, N=0, F=0, T=1
  - S=1: Multiple files touched (justfile, harness.md, new test project) but focused scope
  - I=2: External dependencies — Android Emulator, ADB (no Appium — ADB-scripted approach chosen)
  - D=0: No data or schema changes
  - N=0: Framework decision resolved (ADB-scripted), workshop completed
  - F=0: No performance/security implications
  - T=1: New test infrastructure — emulator management, screenshot capture (ADB-only, no framework overhead)
- **Confidence**: 0.85
- **Assumptions**:
  - Android SDK and emulator images are available on the dev machine
  - ADB (Android Debug Bridge) is installed and accessible
  - .NET MAUI apps can be interacted with via ADB shell commands (tap, swipe, screencap)
  - Emulator boot is reliable enough for automated workflows
- **Dependencies**:
  - Android SDK with system images
  - ADB command-line tool
  - UI automation framework (ADB shell commands — `input tap`, `input swipe`, `screencap`)
- **Risks**:
  - Emulator boot time may be too slow for tight feedback loops (30-60s goal)
  - UI automation frameworks for .NET MAUI may be immature or flaky
  - Screenshot comparison may produce false positives on different emulator configurations
- **Phases**:
  1. Emulator boot automation (ADB + justfile recipes)
  2. App deployment and launch automation
  3. Screenshot capture via ADB
  4. UI smoke test suite (Appium or ADB-scripted)
  5. Harness.md upgrade to L3

## Acceptance Criteria

1. `just boot-emulator` starts an Android emulator and waits until it's ready (boot complete)
2. `just launch-app` builds, deploys, and launches the app on the running emulator
3. `just screenshot` captures the current emulator screen and saves to `scratch/evidence/`
4. `just smoke-test` runs ADB-scripted navigation through 2-3 main screens with screenshot capture at each step
5. Smoke tests produce screenshot evidence in `scratch/evidence/` for each navigation step
6. `docs/project-rules/harness.md` is updated to L3 maturity with new capabilities documented
7. Emulator boot + app launch completes in under 90 seconds from cold start
8. All existing tests (88 unit tests) continue to pass

## Risks & Assumptions

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Emulator boot too slow for feedback loops | Medium | Medium | Use snapshots/quick boot; document expected boot time |
| Appium/MAUI integration issues | Medium | High | ~~Evaluate ADB-scripted approach as fallback~~ **Resolved: using ADB-scripted approach** |
| No emulator installed on dev machine | Low | High | Document setup steps; add `just setup-emulator` recipe |
| Flaky UI tests | Medium | Medium | Keep smoke tests minimal and deterministic |

**Assumptions**:
- Developer has Android SDK installed (prerequisite in README)
- At least one Android system image is available for emulator
- The app launches successfully on emulator (manual verification works today)

## Open Questions

_All original questions resolved during clarification session._

## Testing Strategy

- **Approach**: Manual Only
- **Rationale**: The recipes themselves ARE the test infrastructure — testing them by running them is the natural validation. No new automated tests needed.
- **Validation**: Run each `just` recipe and verify it produces expected output/evidence
- **Existing tests**: All 88 unit tests must continue to pass (`just validate`)

## Documentation Strategy

- **Location**: Update `docs/project-rules/harness.md` and `AGENTS.md` only
- **Rationale**: These are the canonical agent-facing docs. No README changes needed — emulator is a harness concern, not a user-facing feature.

## Clarifications

### Session 2026-05-11

**Q1: Workflow Mode** → **Simple** — single-phase plan, lightweight path. Infrastructure-focused feature doesn't need multi-phase gates.

**Q2: Testing Strategy** → **Manual Only** — verify recipes work by running them. The recipes are the test infrastructure; testing them is meta.

**Q3: UI Automation Framework** → **ADB-scripted** — `adb shell input tap/swipe` + `adb shell screencap` with zero extra dependencies. No Appium, no Node.js, no framework overhead.

**Q4: Emulator Management** → **Auto-create via `just setup-emulator`** — creates AVD if missing, errors if system image not installed (~1GB download should be explicit).

**Q5: Smoke Test Scenarios** → **App launch + navigate 2-3 screens** — tap through main navigation with screenshots. Not full 3-scenario suite (group creation, expense) — keep it simple for first L3 iteration.

**Q6: Documentation Strategy** → **Update harness.md + AGENTS.md only** — no README or new docs needed.

**Domain Review**: Single domain `harness-infrastructure` (informal, existing). No new domains, no boundary changes needed. This is pure infrastructure — no app code changes.

## Workshop Opportunities

| Topic | Type | Why Workshop | Key Questions |
|-------|------|--------------|---------------|
| UI Automation Framework | Integration Pattern | Multiple options (Appium, ADB, MAUI test host) with different tradeoffs for MAUI apps | Which framework is most reliable? What's the maintenance burden? |
| Emulator Lifecycle | CLI Flow | Boot, snapshot, shutdown patterns affect feedback loop speed | Quick boot vs cold boot? Persistent vs ephemeral AVD? |
