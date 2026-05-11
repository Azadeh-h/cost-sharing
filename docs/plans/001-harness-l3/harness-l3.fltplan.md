# ✈️ Flight Plan: Harness L3

**Status**: Landed
**Spec**: `harness-l3-spec.md`
**Plan**: `harness-l3-plan.md`
**Workshop**: `workshops/001-emulator-lifecycle.md`
**Complexity**: CS-2 (small)
**Mode**: Simple (single-phase)
**Branch**: `006-harness-infrastructure`

## Overview
Upgrade agent harness from L2 (build+test) to L3 (emulator+UI+screenshots). Adds 6 justfile recipes for complete emulator lifecycle, plus doc updates.

## Architecture
- **No app code changes** — pure infrastructure (justfile + docs)
- **ADB-scripted** UI automation (no Appium/framework dependencies)
- **Apple Silicon** — uses `arm64-v8a` system images (not x86_64)
- **Quick boot** from snapshots for ~10-15s warm starts

## Key Findings
1. 🔴 System image is `arm64-v8a`, not `x86_64` (workshop corrected)
2. 🟡 `scratch/` not in `.gitignore` — must add
3. 🟡 macOS has no `timeout` cmd — use shell loop
4. ℹ️ Use `monkey` for app launch (avoids crc64 activity hash)

## Tasks (11)
| ID | Task | Status |
|----|------|--------|
| T001 | Add `scratch/` to `.gitignore` | ✅ |
| T002 | Add emulator variables to justfile | ✅ |
| T003 | `setup-emulator` recipe | ✅ |
| T004 | `boot-emulator` recipe | ✅ |
| T005 | `launch-app` recipe | ✅ |
| T006 | `screenshot` recipe | ✅ |
| T007 | `smoke-test` recipe | ✅ |
| T008 | `stop-emulator` recipe | ✅ |
| T009 | Update harness.md → L3 | ✅ |
| T010 | Update AGENTS.md | ✅ |
| T011 | Validate 88 tests pass | ✅ |

## Risks
- Emulator boot time (mitigated: quick boot snapshots)
- ADB tap coordinates for smoke-test (mitigated: empirical + simple navigation)

## Flight Log
- 2026-05-11: Spec created, workshop completed (emulator lifecycle)
- 2026-05-11: Clarification done (6 questions), complexity CS-3→CS-2
- 2026-05-11: Implementation complete — 11/11 tasks, 88/88 tests pass, harness L2→L3

## Status
- [x] Spec created
- [x] Workshop: Emulator Lifecycle
- [x] Clarification complete
- [x] Plan created
- [x] Implementation complete (11/11 tasks)
