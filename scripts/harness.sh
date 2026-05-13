
#!/usr/bin/env bash
set -euo pipefail

TASK="${1:-unspecified task}"
MAX_RETRIES=2
ATTEMPT=0
RESULT="fail"

LOG_DIR="scratch/evidence"
mkdir -p "$LOG_DIR"

LOG_FILE="$LOG_DIR/harness-$(date +%s).log"

echo "=== HARNESS RUN ===" | tee "$LOG_FILE"
echo "Task: $TASK" | tee -a "$LOG_FILE"

run_validate() {
  echo ">> Running validation..." | tee -a "$LOG_FILE"
  if just validate >> "$LOG_FILE" 2>&1 ; then
    return 0
  else
    return 1
  fi
}

domain_check() {
  echo ">> Running domain checks..."

  if dotnet test CostSharingApp/tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj --filter "FullyQualifiedName~DomainInvariant" >> "$LOG_FILE" 2>&1 ; then
    echo ">> Domain checks passed"
    return 0
  else
    echo ">> Domain checks failed"
    return 1
  fi
}


run_review() {
  echo ">> Running AI review sensor..." | tee -a "$LOG_FILE"
  set +e
  bash scripts/review.sh HEAD >> "$LOG_FILE" 2>&1
  rc=$?
  set -e
  if [ $rc -eq 2 ]; then
    echo ">> ⚠️ Review unavailable (non-blocking)" | tee -a "$LOG_FILE"
    return 0
  elif [ $rc -eq 1 ]; then
    echo ">> ❌ Review found critical issues" | tee -a "$LOG_FILE"
    return 0  # non-blocking in v1 — warn only
  else
    echo ">> ✅ Review passed" | tee -a "$LOG_FILE"
    return 0
  fi
}


while [ $ATTEMPT -lt $MAX_RETRIES ]; do
  ATTEMPT=$((ATTEMPT+1))
  echo "== Attempt $ATTEMPT ==" | tee -a "$LOG_FILE"

  if run_validate; then
    run_review
    if domain_check; then
      RESULT="pass"
      break
    else
      RESULT="domain-fail"
      break
    fi
  else
    echo ">> Validation failed" | tee -a "$LOG_FILE"
  fi
done

if [ "$RESULT" != "pass" ]; then
  echo ">> Harness failed after $ATTEMPT attempts" | tee -a "$LOG_FILE"
fi

# Produce structured output
OUTPUT_FILE="$LOG_DIR/result-$(date +%s).json"

cat <<EOF > "$OUTPUT_FILE"
{
  "task": "$TASK",
  "result": "$RESULT",
  "attempts": $ATTEMPT,
  "log": "$LOG_FILE"
}
EOF

echo "=== HARNESS RESULT ==="
cat "$OUTPUT_FILE"

if [ "$RESULT" == "pass" ]; then
  exit 0
else
  exit 1
fi
