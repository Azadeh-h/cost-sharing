
#!/usr/bin/env bash
# harness.sh — Orchestrated validation loop for coding agents
#
# Chains: validate → review → domain check
# Retries on validation failure (up to 2 attempts)
# Produces structured JSON result
#
# All steps use justfile recipes (which are wrapped by agent-wrap.sh)
# so failure output is already agent-friendly JSON.

set -uo pipefail

TASK="${1:-validate}"
MAX_RETRIES=2
ATTEMPT=0
RESULT="fail"
STEPS_RUN=()
STEPS_STATUS=()

LOG_DIR="scratch/evidence"
mkdir -p "$LOG_DIR"
TIMESTAMP=$(date +%s)
LOG_FILE="$LOG_DIR/harness-${TIMESTAMP}.log"

echo "=== HARNESS RUN ===" | tee "$LOG_FILE"
echo "Task: $TASK" | tee -a "$LOG_FILE"

run_step() {
  local name="$1"
  shift
  echo ">> Running ${name}..." | tee -a "$LOG_FILE"
  STEPS_RUN+=("$name")

  set +e
  step_output=$("$@" 2>&1)
  rc=$?
  set -e

  echo "$step_output" >> "$LOG_FILE"

  if [ $rc -eq 0 ] || [ $rc -eq 2 ]; then
    local status="pass"
    [ $rc -eq 2 ] && status="self-healed"
    echo ">> ✅ ${name}: ${status}" | tee -a "$LOG_FILE"
    STEPS_STATUS+=("$status")
    return 0
  else
    echo ">> ❌ ${name}: failed" | tee -a "$LOG_FILE"
    # If step output is JSON (from agent-wrap), show it for agent consumption
    if echo "$step_output" | grep -q '"error_type"'; then
      echo "$step_output" | tail -20
    fi
    STEPS_STATUS+=("failed")
    return 1
  fi
}

run_review() {
  echo ">> Running AI review sensor..." | tee -a "$LOG_FILE"
  STEPS_RUN+=("review")

  set +e
  bash scripts/review.sh HEAD >> "$LOG_FILE" 2>&1
  rc=$?
  set -e

  if [ $rc -eq 2 ]; then
    echo ">> ⚠️ Review unavailable (non-blocking)" | tee -a "$LOG_FILE"
    STEPS_STATUS+=("unavailable")
  elif [ $rc -eq 1 ]; then
    echo ">> ⚠️ Review found issues (non-blocking)" | tee -a "$LOG_FILE"
    STEPS_STATUS+=("warnings")
  else
    echo ">> ✅ Review passed" | tee -a "$LOG_FILE"
    STEPS_STATUS+=("pass")
  fi
  return 0  # always non-blocking
}

# --- Main loop ---

while [ $ATTEMPT -lt $MAX_RETRIES ]; do
  ATTEMPT=$((ATTEMPT+1))
  echo "== Attempt $ATTEMPT ==" | tee -a "$LOG_FILE"

  if run_step "validate" just validate; then
    run_review
    if run_step "domain-check" just test; then
      RESULT="pass"
      break
    else
      RESULT="domain-fail"
      break
    fi
  else
    echo ">> Retrying..." | tee -a "$LOG_FILE"
  fi
done

if [ "$RESULT" != "pass" ] && [ "$RESULT" != "domain-fail" ]; then
  echo ">> Harness failed after $ATTEMPT attempts" | tee -a "$LOG_FILE"
fi

# --- Build structured result ---

steps_json=""
for i in "${!STEPS_RUN[@]}"; do
  [ -n "$steps_json" ] && steps_json+=","
  steps_json+="{\"step\":\"${STEPS_RUN[$i]}\",\"status\":\"${STEPS_STATUS[$i]}\"}"
done

cat <<EOF | tee -a "$LOG_FILE"
=== HARNESS RESULT ===
{
  "task": "$TASK",
  "result": "$RESULT",
  "attempts": $ATTEMPT,
  "steps": [${steps_json}],
  "log": "$LOG_FILE"
}
EOF

if [ "$RESULT" == "pass" ]; then
  exit 0
else
  exit 1
fi
