# ✈️ Flight Plan: L4 Self-Healing Harness

**Status**: Ready
**Spec**: `docs/plans/002-harness-l4/harness-l4-spec.md`
**Plan**: `docs/plans/002-harness-l4/harness-l4-plan.md`
**Branch**: `006-harness-infrastructure`
**Complexity**: CS-2 (small)
**Mode**: Simple (single phase, 11 tasks)

## Mission

Upgrade harness from L3 → L4 by adding self-healing recovery logic to all justfile recipes. Agents should never get stuck on environment drift.

## Key Deliverables

1. Self-healing `validate`/`check` recipes (stale build + NuGet recovery)
2. Orphan emulator cleanup in `boot-emulator`
3. Auto-install system image in `setup-emulator`
4. Crash-retry in `launch-app`
5. New `just doctor` diagnostic recipe
6. Structured exit codes (0/1/2)
7. Updated harness.md (L3 → L4) and AGENTS.md

## Key Findings

- F01: `set -euo pipefail` conflicts with retry — scope `set +e` around recovery blocks
- F02: `sdkmanager` not on PATH — resolve from SDK tree
- F03: Orphan kill must match AVD name — don't kill user's emulator

## Current Phase

🟢 Landed — all 11 tasks complete, 88 tests pass, `just doctor` operational
