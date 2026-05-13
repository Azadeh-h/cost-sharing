#!/usr/bin/env bash
# AI Code Review Sensor — Inferential feedback for the agent harness
# Reviews changed files for semantic issues that tests/linters cannot catch.
#
# Exit codes: 0 = approve/concern, 1 = critical findings, 2 = could not complete
# Timeout: 30 seconds

set -uo pipefail

EVIDENCE_DIR="scratch/evidence"
mkdir -p "$EVIDENCE_DIR"

TIMESTAMP=$(date +%s)
OUTPUT_FILE="$EVIDENCE_DIR/review-$TIMESTAMP.json"

# Determine changed files
DIFF_BASE="${1:-HEAD}"
CHANGED_FILES=$(git diff --name-only "$DIFF_BASE" 2>/dev/null || git diff --name-only --cached 2>/dev/null || echo "")

if [ -z "$CHANGED_FILES" ]; then
    # Nothing to review
    cat <<EOF > "$OUTPUT_FILE"
{
  "review": {
    "verdict": "approve",
    "findings": [],
    "summary": "Nothing to review — no changed files detected",
    "files_reviewed": 0,
    "duration_ms": 0
  }
}
EOF
    echo "✅ Nothing to review"
    cat "$OUTPUT_FILE"
    exit 0
fi

FILE_COUNT=$(echo "$CHANGED_FILES" | wc -l | tr -d ' ')
echo "🔍 Reviewing $FILE_COUNT changed file(s)..."

# Collect diff content for review context
DIFF_CONTENT=$(git diff "$DIFF_BASE" 2>/dev/null || git diff --cached 2>/dev/null || echo "no diff available")

# Build review prompt
REVIEW_PROMPT="You are reviewing code changes in a .NET MAUI cost-sharing app.

DOMAIN INVARIANTS (must always hold):
- Total expense = sum of all participant splits
- Debt simplification must preserve net balances
- Settlement must reduce outstanding balances (never create phantom debt)
- Decimal calculations must be deterministic (no lost pennies)
- No negative residual debt

ARCHITECTURE RULES:
- CostSharing.Core must NOT reference MAUI types
- Services must use DI (no manual 'new' for service dependencies)
- Async pattern used throughout — no sync-over-async

CHECK FOR:
1. Logic errors (inverted conditions, off-by-one, unreachable paths)
2. Domain violations (splits not summing, phantom balances, rounding issues)
3. Boundary breaks (Core→MAUI references, bypassing interfaces)
4. Pattern breaks (manual instantiation, missing async, inconsistent null checks)
5. Incomplete changes (service changed but test not updated)

DO NOT FLAG:
- Style/formatting issues (StyleCop handles this)
- Null warnings (.NET analyzers handle this)
- Naming conventions (EditorConfig handles this)

Changed files:
$CHANGED_FILES

Diff:
$DIFF_CONTENT

Return ONLY valid JSON in this exact format:
{
  \"review\": {
    \"verdict\": \"approve | concern | reject\",
    \"findings\": [
      {
        \"severity\": \"critical | warning | info\",
        \"file\": \"path/to/file\",
        \"line\": 0,
        \"category\": \"logic | domain | boundary | pattern | incomplete\",
        \"message\": \"Description\",
        \"suggestion\": \"Actionable fix instruction\"
      }
    ],
    \"summary\": \"One-line summary\"
  }
}

If no issues found, return verdict 'approve' with empty findings array."

# Execute review with timeout
START_MS=$(($(date +%s) * 1000))

set +e
REVIEW_RESULT=$(echo "$REVIEW_PROMPT" | timeout 30 gh copilot suggest -t shell "Review this code diff and return JSON findings" 2>/dev/null)
REVIEW_EXIT=$?
set -e

END_MS=$(($(date +%s) * 1000))
DURATION=$((END_MS - START_MS))

# Handle timeout or failure
if [ $REVIEW_EXIT -eq 124 ]; then
    echo "⚠️ Review timed out (30s limit)"
    cat <<EOF > "$OUTPUT_FILE"
{
  "review": {
    "verdict": "approve",
    "findings": [],
    "summary": "Review timed out — continuing without findings",
    "files_reviewed": $FILE_COUNT,
    "duration_ms": 30000,
    "error": "timeout"
  }
}
EOF
    cat "$OUTPUT_FILE"
    exit 2
fi

if [ $REVIEW_EXIT -ne 0 ] || [ -z "$REVIEW_RESULT" ]; then
    echo "⚠️ Review could not complete (exit: $REVIEW_EXIT)"
    cat <<EOF > "$OUTPUT_FILE"
{
  "review": {
    "verdict": "approve",
    "findings": [],
    "summary": "Review unavailable — continuing without findings",
    "files_reviewed": $FILE_COUNT,
    "duration_ms": $DURATION,
    "error": "review_failed"
  }
}
EOF
    cat "$OUTPUT_FILE"
    exit 2
fi

# Try to extract JSON from review result
JSON_RESULT=$(echo "$REVIEW_RESULT" | grep -Pzo '\{[\s\S]*\}' 2>/dev/null || echo "$REVIEW_RESULT")

# Validate we got something JSON-like
if echo "$JSON_RESULT" | python3 -c "import sys,json; json.load(sys.stdin)" 2>/dev/null; then
    echo "$JSON_RESULT" > "$OUTPUT_FILE"
else
    # Wrap raw output as best-effort
    cat <<EOF > "$OUTPUT_FILE"
{
  "review": {
    "verdict": "concern",
    "findings": [
      {
        "severity": "info",
        "file": "",
        "line": 0,
        "category": "logic",
        "message": "Review produced non-JSON output — manual inspection recommended",
        "suggestion": "Run 'just review' again or inspect output manually"
      }
    ],
    "summary": "Review completed but output was not structured JSON",
    "files_reviewed": $FILE_COUNT,
    "duration_ms": $DURATION,
    "raw_output": true
  }
}
EOF
fi

echo ""
echo "📋 Review result:"
cat "$OUTPUT_FILE"

# Determine exit code from verdict
VERDICT=$(python3 -c "import sys,json; d=json.load(open('$OUTPUT_FILE')); print(d.get('review',{}).get('verdict','approve'))" 2>/dev/null || echo "approve")

case "$VERDICT" in
    reject)
        echo ""
        echo "❌ Review: REJECT — critical findings detected"
        exit 1
        ;;
    concern)
        echo ""
        echo "⚠️ Review: CONCERN — warnings found (non-blocking)"
        exit 0
        ;;
    *)
        echo ""
        echo "✅ Review: APPROVE"
        exit 0
        ;;
esac
