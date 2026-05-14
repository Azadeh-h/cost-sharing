# ✈️ Flight Plan: AI Code Review Sensor

**Plan**: 004-ai-code-review-sensor
**Status**: Specifying
**Complexity**: CS-2 (small) — P=4
**Confidence**: 0.80

## Mission

Add an automated inferential feedback sensor that reviews code changes for semantic correctness, domain violations, and pattern breaks — filling the gap between computational sensors (tests/linters) and human review.

## Concept (Fowler's Framework)

```
BEFORE (current):
  Guides ──▶ Agent generates code ──▶ Computational sensors ──▶ Human review
  (AGENTS.md)                         (tests, StyleCop)         (manual)

AFTER (with this feature):
  Guides ──▶ Agent generates code ──▶ Computational sensors ──▶ INFERENTIAL SENSOR ──▶ Human review
  (AGENTS.md)                         (tests, StyleCop)         (AI review, auto)      (reduced toil)
```

## Phases

| # | Phase | Scope | Files |
|---|-------|-------|-------|
| 1 | Agent definition + recipe | `reviewer.agent.md` + `just review` recipe | 2 new, 1 modified |
| 2 | Harness integration | Auto-run in `scripts/harness.sh` after validate | 1 modified |

## Key Design Decisions

- **Local-first** — no CI required (no GitHub Actions exist yet)
- **Non-blocking** — warns but doesn't fail pipeline in v1
- **Diff-scoped** — reviews only changed files, not full codebase
- **LLM-optimised output** — findings written as fix-instructions ("positive prompt injection")
- **Structured evidence** — JSON output to `scratch/evidence/`

## Files Affected

| File | Action | Phase |
|------|--------|-------|
| `.github/agents/reviewer.agent.md` | Create | 1 |
| `justfile` | Add `review` recipe | 1 |
| `scripts/harness.sh` | Add `run_review()` step | 2 |
| `AGENTS.md` | Update Multi-Agent Flow | 2 |
| `docs/project-rules/harness.md` | Update execution order | 2 |

## Open Questions (3)

1. Review scope: all files or only allowed write scope?
2. Timeout: 30s or 60s?
3. Confidence levels in findings?

## Next Steps

1. Run `/plan-2-clarify` to resolve 3 open questions
2. Then `/plan-3-architect` for implementation plan
3. Or skip to `/plan-6-implement` (CS-2 is small enough for direct implementation)

---
*Generated: 2026-05-13T11:46:00+10:00*
