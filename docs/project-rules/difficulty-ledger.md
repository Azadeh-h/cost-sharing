# Difficulty Ledger

Track friction, fix it, compound velocity. Every entry here is a gift to future sessions.

**Rules**:
1. When you hit friction, log it immediately (don't wait)
2. Status must progress: `open` → `mitigated` → `encoded`
3. `mitigated` = workaround exists. `encoded` = automated fix in place
4. Link to the fix (commit, recipe, script) when encoding

## Ledger

| # | Date | Difficulty | Impact | Status | Resolution |
|---|------|-----------|--------|--------|------------|
| D001 | 2026-05-11 | Full MAUI app build fails on macOS due to Xcode version mismatch (needs 26.0, have 26.2) — iOS/MacCatalyst targets block the build | HIGH | open | Workaround: build tests only (`dotnet test`). Fix: remove non-Android targets (branch `004-gmail-invite-sync`) |
| D002 | 2026-05-11 | No agent bootstrap doc — new sessions must explore codebase from scratch | MEDIUM | encoded | Created `AGENTS.md` with quick-start, structure, and conventions |
| D003 | 2026-05-11 | Specs describe aspirational architecture (backend API, React, Google Drive) that doesn't exist — agents waste time investigating phantom components | MEDIUM | encoded | Added warning in `AGENTS.md` |
| D004 | 2026-05-11 | No automation recipes — agents must discover and type dotnet CLI commands manually | MEDIUM | open | Plan: create `justfile` with named commands (test, build, validate, etc.) |
| D005 | 2026-05-11 | Test count in README says 40, actual count is 88 — misleading for agents | LOW | open | Plan: fix README test count |

## Velocity Log

Track how long phases take to measure compounding.

| Date | Task | Duration | Notes |
|------|------|----------|-------|
| 2026-05-11 | Initial harness exploration + AGENTS.md + difficulty ledger | ~15 min | First session — baseline established |

## Wishlist (Magic Wand)

Things that would make development faster if they existed:

- [ ] `justfile` with named recipes (test, validate, build-app, clean, check)
- [ ] Pre-commit hook that runs tests
- [ ] CI pipeline for automated PR validation
- [ ] Appium/UI test harness for L3 maturity
- [ ] Seed data script for testing with realistic groups/expenses
