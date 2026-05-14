#!/usr/bin/env bash
# drift-scan.sh — Continuous drift detection with 4 sensors
#
# Usage: scripts/drift-scan.sh
#
# Sensors:
#   1. dependencies — outdated/vulnerable NuGet packages
#   2. coverage     — per-file line coverage for CostSharing.Core
#   3. dead_code    — unreferenced interfaces (with DI exclusion list)
#   4. doc_drift    — AGENTS.md/harness.md facts vs reality
#
# Output: JSON report to stdout with per-sensor status + overall verdict.

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP_DIR="$REPO_ROOT/CostSharingApp"
CORE_DIR="$APP_DIR/src/CostSharing.Core"
TEST_DIR="$APP_DIR/tests/CostSharingApp.Tests"
MAUI_DIR="$APP_DIR/src/CostSharingApp"

# DI-registered interfaces — excluded from dead code detection
DI_EXCLUSIONS=(
  "IDriveAuthService"
  "IDriveSyncService"
  "IOfflineQueueService"
  "IConflictResolver"
  "IGmailInvitationService"
  "IInvitationLinkingService"
)

# Temp dir for intermediate results
TMPDIR_DRIFT=$(mktemp -d)
trap 'rm -rf "$TMPDIR_DRIFT"' EXIT

# ─── Sensor 1: Dependencies ─────────────────────────────────────────────────

sensor_dependencies() {
  local status="✅"
  local findings="[]"
  local outdated_count=0
  local vulnerable_count=0

  # Check outdated packages (strip ANSI escape codes from dotnet output)
  local outdated_raw=""
  if outdated_raw=$(cd "$APP_DIR" && dotnet list package --outdated 2>&1 | sed $'s/\033\[[0-9;]*m//g'); then
    local outdated_lines
    outdated_lines=$(echo "$outdated_raw" | grep -E "^\s+>" || true)
    if [ -n "$outdated_lines" ]; then
      outdated_count=$(echo "$outdated_lines" | wc -l | tr -d ' ')
      findings=$(echo "$outdated_lines" | head -20 | while IFS= read -r line; do
        pkg=$(echo "$line" | awk '{print $2}')
        current=$(echo "$line" | awk '{print $3}')
        latest=$(echo "$line" | awk '{print $NF}')
        printf '{"package":"%s","current":"%s","latest":"%s","type":"outdated"},' \
          "$pkg" "$current" "$latest"
      done | sed '$ s/,$//')
      findings="[${findings}]"
      status="⚠️"
    fi
  else
    if echo "$outdated_raw" | grep -qiE "Unable to load|no internet|timeout|network"; then
      findings='[{"message":"Dependency scan skipped — network unavailable","type":"skipped"}]'
      status="⚠️"
      printf '{"sensor":"dependencies","status":"%s","count":%d,"findings":%s}' \
        "$status" 0 "$findings"
      return
    fi
  fi

  # Check vulnerable packages (strip ANSI escape codes)
  local vuln_raw=""
  if vuln_raw=$(cd "$APP_DIR" && dotnet list package --vulnerable 2>&1 | sed $'s/\033\[[0-9;]*m//g'); then
    local vuln_lines
    vuln_lines=$(echo "$vuln_raw" | grep -E "^\s+>" || true)
    if [ -n "$vuln_lines" ]; then
      vulnerable_count=$(echo "$vuln_lines" | wc -l | tr -d ' ')
      local vuln_findings
      vuln_findings=$(echo "$vuln_lines" | head -10 | while IFS= read -r line; do
        pkg=$(echo "$line" | awk '{print $2}')
        severity=$(echo "$line" | awk '{print $NF}')
        printf '{"package":"%s","severity":"%s","type":"vulnerable"},' \
          "$pkg" "$severity"
      done | sed '$ s/,$//')

      if [ "$findings" = "[]" ]; then
        findings="[${vuln_findings}]"
      else
        # Merge arrays
        findings="${findings%]},$vuln_findings]"
      fi
      status="❌"
    fi
  fi

  local total=$((outdated_count + vulnerable_count))
  if [ $total -eq 0 ]; then
    findings='[]'
    status="✅"
  fi

  printf '{"sensor":"dependencies","status":"%s","outdated":%d,"vulnerable":%d,"findings":%s}' \
    "$status" "$outdated_count" "$vulnerable_count" "$findings"
}

# ─── Sensor 2: Coverage Quality ──────────────────────────────────────────────

sensor_coverage() {
  local status="✅"
  local findings="[]"
  local coverage_dir="$TMPDIR_DRIFT/coverage"
  mkdir -p "$coverage_dir"

  # Run tests with coverage collection
  local test_out=""
  if ! test_out=$(dotnet test "$TEST_DIR/$( basename "$TEST_DIR").csproj" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$coverage_dir" \
    --verbosity quiet 2>&1); then
    printf '{"sensor":"coverage","status":"❌","message":"Tests failed during coverage collection","findings":[]}'
    return
  fi

  # Find the cobertura XML
  local cob_file
  cob_file=$(find "$coverage_dir" -name "coverage.cobertura.xml" -type f 2>/dev/null | head -1)

  if [ -z "$cob_file" ]; then
    printf '{"sensor":"coverage","status":"⚠️","message":"No coverage report generated","findings":[]}'
    return
  fi

  # Parse cobertura XML for per-file line-rate
  # Source is CostSharing.Core, so all filenames are relative to it
  findings=$(grep -oE 'filename="[^"]*"[^>]*line-rate="[^"]*"' "$cob_file" | while IFS= read -r match; do
    fname=$(echo "$match" | grep -oE 'filename="[^"]*"' | sed 's/filename="//;s/"//')
    rate=$(echo "$match" | grep -oE 'line-rate="[^"]*"' | sed 's/line-rate="//;s/"//')
    pct=$(echo "$rate" | awk '{printf "%.1f", $1 * 100}')
    printf '{"file":"%s","coverage_pct":%s},' "$fname" "$pct"
  done | sed '$ s/,$//')

  if [ -z "$findings" ]; then
    findings="[]"
    status="⚠️"
  else
    findings="[${findings}]"
    # Check if any file is below 50%
    if echo "$findings" | grep -qE '"coverage_pct":[0-4][0-9]?\.' ; then
      status="⚠️"
    fi
  fi

  # Get overall line-rate for CostSharing.Core
  local overall_rate
  overall_rate=$(grep -oE '<package[^>]*name="CostSharing.Core"[^>]*line-rate="[^"]*"' "$cob_file" 2>/dev/null | \
    grep -oE 'line-rate="[^"]*"' | sed 's/line-rate="//;s/"//' | head -1 || true)

  if [ -z "$overall_rate" ]; then
    # Fallback: use first coverage summary line-rate
    overall_rate=$(grep -oE '<coverage[^>]*line-rate="[^"]*"' "$cob_file" 2>/dev/null | \
      grep -oE 'line-rate="[^"]*"' | sed 's/line-rate="//;s/"//' | head -1 || echo "0")
  fi

  local overall_pct
  overall_pct=$(echo "$overall_rate" | awk '{printf "%.1f", $1 * 100}')

  printf '{"sensor":"coverage","status":"%s","overall_pct":%s,"findings":%s}' \
    "$status" "$overall_pct" "$findings"
}

# ─── Sensor 3: Dead Code (Interfaces) ───────────────────────────────────────

sensor_dead_code() {
  local status="✅"
  local findings=""
  local dead_count=0

  local interface_dir="$CORE_DIR/Interfaces"
  if [ ! -d "$interface_dir" ]; then
    printf '{"sensor":"dead_code","status":"⚠️","message":"No Interfaces directory found","findings":[]}'
    return
  fi

  for ifile in "$interface_dir"/*.cs; do
    [ -f "$ifile" ] || continue
    local iname
    iname=$(basename "$ifile" .cs)

    # Check if this interface is in the DI exclusion list
    local excluded=false
    for excl in "${DI_EXCLUSIONS[@]}"; do
      if [ "$iname" = "$excl" ]; then
        excluded=true
        break
      fi
    done

    # Count references in test directory
    local test_refs=0
    test_refs=$(grep -rl "$iname" "$TEST_DIR" --include="*.cs" 2>/dev/null | wc -l | tr -d ' ')

    # Count references in MAUI app directory (excluding the interface file itself)
    local app_refs=0
    app_refs=$(grep -rl "$iname" "$MAUI_DIR" --include="*.cs" 2>/dev/null | wc -l | tr -d ' ')

    # Count references in Core directory (excluding the interface file itself)
    local core_refs=0
    core_refs=$(grep -rl "$iname" "$CORE_DIR" --include="*.cs" 2>/dev/null | \
      grep -v "$ifile" | wc -l | tr -d ' ')

    local total_refs=$((test_refs + app_refs + core_refs))

    if [ "$excluded" = true ]; then
      # DI-registered: report but don't flag as dead
      if [ $test_refs -eq 0 ]; then
        findings="${findings}{\"interface\":\"$iname\",\"test_refs\":$test_refs,\"app_refs\":$app_refs,\"core_refs\":$core_refs,\"di_registered\":true,\"status\":\"untested\"},"
      fi
    else
      # Not DI-registered: if zero total refs, it's dead
      if [ $total_refs -eq 0 ]; then
        dead_count=$((dead_count + 1))
        findings="${findings}{\"interface\":\"$iname\",\"test_refs\":0,\"app_refs\":0,\"core_refs\":0,\"di_registered\":false,\"status\":\"dead\"},"
      fi
    fi
  done

  findings=$(echo "$findings" | sed 's/,$//')
  if [ -z "$findings" ]; then
    findings="[]"
  else
    findings="[${findings}]"
  fi

  if [ $dead_count -gt 0 ]; then
    status="❌"
  elif echo "$findings" | grep -q "untested"; then
    status="⚠️"
  fi

  printf '{"sensor":"dead_code","status":"%s","dead_count":%d,"findings":%s}' \
    "$status" "$dead_count" "$findings"
}

# ─── Sensor 4: Doc Drift ────────────────────────────────────────────────────

sensor_doc_drift() {
  local status="✅"
  local findings=""
  local drift_count=0

  # --- Check 1: Test count in AGENTS.md ---
  local agents_file="$REPO_ROOT/AGENTS.md"
  if [ -f "$agents_file" ]; then
    # Get actual test count from dotnet test
    local actual_tests
    actual_tests=$(dotnet test "$TEST_DIR/$(basename "$TEST_DIR").csproj" --verbosity quiet 2>&1 | \
      grep -oiE 'Total:\s+[0-9]+' | grep -oE '[0-9]+' | head -1 || echo "0")

    # Find documented test counts in AGENTS.md
    local doc_tests
    doc_tests=$(grep -oE '[0-9]+ (tests|xUnit tests)' "$agents_file" | grep -oE '^[0-9]+' | head -1 || echo "")

    if [ -n "$doc_tests" ] && [ "$doc_tests" != "$actual_tests" ]; then
      drift_count=$((drift_count + 1))
      findings="${findings}{\"type\":\"test_count\",\"file\":\"AGENTS.md\",\"documented\":\"$doc_tests\",\"actual\":\"$actual_tests\"},"
    fi
  fi

  # --- Check 2: Test count in harness.md ---
  local harness_file="$REPO_ROOT/docs/project-rules/harness.md"
  if [ -f "$harness_file" ]; then
    local harness_tests
    harness_tests=$(grep -oE '[0-9]+ xUnit tests' "$harness_file" | grep -oE '^[0-9]+' | head -1 || echo "")

    if [ -n "$harness_tests" ] && [ -n "$actual_tests" ] && [ "$harness_tests" != "$actual_tests" ]; then
      drift_count=$((drift_count + 1))
      findings="${findings}{\"type\":\"test_count\",\"file\":\"harness.md\",\"documented\":\"$harness_tests\",\"actual\":\"$actual_tests\"},"
    fi
  fi

  # --- Check 3: Justfile command count vs harness.md ---
  local actual_commands
  actual_commands=$(cd "$REPO_ROOT" && just --list 2>/dev/null | grep -cE '^\s+\w' || echo "0")

  if [ -f "$harness_file" ]; then
    local doc_commands
    doc_commands=$(grep -oE '[0-9]+ named commands' "$harness_file" | grep -oE '^[0-9]+' | head -1 || echo "")

    if [ -n "$doc_commands" ] && [ "$doc_commands" != "$actual_commands" ]; then
      drift_count=$((drift_count + 1))
      findings="${findings}{\"type\":\"command_count\",\"file\":\"harness.md\",\"documented\":\"$doc_commands\",\"actual\":\"$actual_commands\"},"
    fi
  fi

  # --- Check 4: Mutation baseline in AGENTS.md ---
  if [ -f "$agents_file" ]; then
    local doc_mutation
    doc_mutation=$(grep -oE 'Baseline score.*: [0-9]+\.[0-9]+%' "$agents_file" | grep -oE '[0-9]+\.[0-9]+' | head -1 || echo "")
    # Just verify it exists (we don't re-run mutation to check)
    if [ -n "$doc_mutation" ]; then
      findings="${findings}{\"type\":\"mutation_baseline\",\"file\":\"AGENTS.md\",\"documented\":\"${doc_mutation}%\",\"status\":\"present\"},"
    fi
  fi

  findings=$(echo "$findings" | sed 's/,$//')
  if [ -z "$findings" ]; then
    findings="[]"
  else
    findings="[${findings}]"
  fi

  if [ $drift_count -gt 2 ]; then
    status="❌"
  elif [ $drift_count -gt 0 ]; then
    status="⚠️"
  fi

  printf '{"sensor":"doc_drift","status":"%s","drift_count":%d,"findings":%s}' \
    "$status" "$drift_count" "$findings"
}

# ─── Unified Report ─────────────────────────────────────────────────────────

main() {
  echo "🔍 Running drift detection sensors..." >&2

  echo "  [1/4] Dependencies..." >&2
  local dep_result
  dep_result=$(sensor_dependencies)

  echo "  [2/4] Coverage..." >&2
  local cov_result
  cov_result=$(sensor_coverage)

  echo "  [3/4] Dead code..." >&2
  local dc_result
  dc_result=$(sensor_dead_code)

  echo "  [4/4] Doc drift..." >&2
  local dd_result
  dd_result=$(sensor_doc_drift)

  # Count statuses
  local healthy=0 drifting=0 critical=0
  for result in "$dep_result" "$cov_result" "$dc_result" "$dd_result"; do
    local s
    s=$(echo "$result" | grep -oE '"status":"[^"]*"' | head -1 | sed 's/"status":"//;s/"//')
    case "$s" in
      "✅") healthy=$((healthy + 1)) ;;
      "⚠️") drifting=$((drifting + 1)) ;;
      "❌") critical=$((critical + 1)) ;;
    esac
  done

  local verdict="✅ healthy"
  if [ $critical -gt 0 ]; then
    verdict="❌ critical"
  elif [ $drifting -gt 0 ]; then
    verdict="⚠️ drifting"
  fi

  local scan_time
  scan_time=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

  # Emit unified JSON report
  cat <<EOF
{
  "scan_time": "$scan_time",
  "sensors": {
    "dependencies": $dep_result,
    "coverage": $cov_result,
    "dead_code": $dc_result,
    "doc_drift": $dd_result
  },
  "summary": {
    "healthy": $healthy,
    "drifting": $drifting,
    "critical": $critical,
    "verdict": "$verdict"
  }
}
EOF

  echo "" >&2
  echo "Drift scan complete: $healthy ✅ | $drifting ⚠️ | $critical ❌ → $verdict" >&2
}

main
