#!/usr/bin/env bash
# agent-wrap.sh — Agent-friendly error wrapper for harness commands
#
# Usage: scripts/agent-wrap.sh <command-name> <actual-command...>
#
# On success: passes stdout through unchanged
# On failure: emits structured JSON error to stdout, full log to file
#
# Error types detected: build, test, restore, mutation, unknown

set -uo pipefail

COMMAND_NAME="${1:?Usage: agent-wrap.sh <name> <command...>}"
shift

LOG_DIR="scratch/evidence"
mkdir -p "$LOG_DIR"
TIMESTAMP=$(date +%s)
LOG_FILE="$LOG_DIR/${COMMAND_NAME}-${TIMESTAMP}.log"

# Run the command, capture everything
set +e
output=$("$@" 2>&1)
exit_code=$?
set -e

echo "$output" > "$LOG_FILE"

# Success — pass through, no wrapping
if [ $exit_code -eq 0 ] || [ $exit_code -eq 2 ]; then
  echo "$output"
  exit $exit_code
fi

# --- Failure path: classify and structure ---

error_type="unknown"
errors_json="[]"
summary=""
suggestion=""

# Detect build errors (CS*, MSB*, NETSDK*)
build_errors=$(echo "$output" | grep -E "error (CS|MSB|NETSDK)[0-9]+" || true)
if [ -n "$build_errors" ]; then
  error_type="build"
  error_count=$(echo "$build_errors" | wc -l | tr -d ' ')

  # Parse up to 10 errors into JSON array
  errors_json=$(echo "$build_errors" | head -10 | while IFS= read -r line; do
    file=$(echo "$line" | grep -oE '[^ ]+\.(cs|csproj)\([0-9]+,[0-9]+\)' | head -1 || echo "")
    code=$(echo "$line" | grep -oE '(CS|MSB|NETSDK)[0-9]+' | head -1 || echo "")
    msg=$(echo "$line" | sed 's/.*error [A-Z]*[0-9]*: //' | head -c 200)

    if [ -n "$file" ]; then
      filepath=$(echo "$file" | sed 's/(.*//')
      lineno=$(echo "$file" | grep -oE '\([0-9]+' | tr -d '(' | head -1)
      printf '{"file":"%s","line":%s,"code":"%s","message":"%s"}' \
        "$filepath" "${lineno:-0}" "$code" "$msg"
    else
      printf '{"file":"unknown","line":0,"code":"%s","message":"%s"}' \
        "$code" "$msg"
    fi
    echo ","
  done | sed '$ s/,$//')
  errors_json="[${errors_json}]"

  summary="${error_count} build error(s)"
  suggestion="Fix the build errors above. Run 'just build' to verify."
fi

# Detect test failures
if [ "$error_type" = "unknown" ]; then
  test_failures=$(echo "$output" | grep -E "Failed!" || true)
  if [ -n "$test_failures" ]; then
    error_type="test"

    failed_tests=$(echo "$output" | grep -E "^\s+Failed\s+" | head -10 || true)
    fail_count=$(echo "$output" | grep -oE 'Failed:\s+[0-9]+' | grep -oE '[0-9]+' | head -1 || echo "?")

    errors_json=$(echo "$failed_tests" | while IFS= read -r line; do
      test_name=$(echo "$line" | sed 's/.*Failed //' | head -c 200)
      printf '{"test":"%s","message":"assertion failed"}' "$test_name"
      echo ","
    done | sed '$ s/,$//')
    errors_json="[${errors_json}]"

    summary="${fail_count} test(s) failed"
    suggestion="Check failing test assertions. Run 'just test' for details."
  fi
fi

# Detect NuGet restore errors
if [ "$error_type" = "unknown" ]; then
  restore_errors=$(echo "$output" | grep -E "error NU[0-9]+" || true)
  if [ -n "$restore_errors" ]; then
    error_type="restore"
    error_count=$(echo "$restore_errors" | wc -l | tr -d ' ')

    errors_json=$(echo "$restore_errors" | head -5 | while IFS= read -r line; do
      code=$(echo "$line" | grep -oE 'NU[0-9]+' | head -1 || echo "")
      msg=$(echo "$line" | sed 's/.*error NU[0-9]*: //' | head -c 200)
      printf '{"code":"%s","message":"%s"}' "$code" "$msg"
      echo ","
    done | sed '$ s/,$//')
    errors_json="[${errors_json}]"

    summary="${error_count} NuGet error(s)"
    suggestion="Run 'just check' to auto-heal NuGet issues."
  fi
fi

# Detect mutation threshold failures
if [ "$error_type" = "unknown" ]; then
  mutation_fail=$(echo "$output" | grep -iE "mutation score.*below|threshold.*break" || true)
  if [ -n "$mutation_fail" ]; then
    error_type="mutation"
    score=$(echo "$output" | grep -oE '[0-9]+\.[0-9]+\s*%' | tail -1 || echo "unknown")
    errors_json="[{\"score\":\"${score}\",\"message\":\"Mutation score below break threshold\"}]"
    summary="Mutation score ${score} below break threshold"
    suggestion="Strengthen test assertions for surviving mutants. Run 'just mutate' for the full report."
  fi
fi

# Fallback: grab last 5 lines as error context
if [ "$error_type" = "unknown" ]; then
  last_lines=$(echo "$output" | tail -5 | tr '\n' ' ' | head -c 500)
  errors_json="[{\"message\":\"$(echo "$last_lines" | sed 's/"/\\"/g')\"}]"
  summary="Command failed with exit code ${exit_code}"
  suggestion="Check full log at ${LOG_FILE}"
fi

# Emit structured JSON
cat <<EOF
{
  "command": "${COMMAND_NAME}",
  "status": "failed",
  "exit_code": ${exit_code},
  "error_type": "${error_type}",
  "errors": ${errors_json},
  "summary": "${summary}",
  "suggestion": "${suggestion}",
  "full_log": "${LOG_FILE}"
}
EOF

exit $exit_code
