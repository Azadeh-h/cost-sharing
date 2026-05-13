# Cost-Sharing App — Agent Harness Commands (L4 Self-Healing)
# See docs/project-rules/harness.md for full harness documentation
#
# Exit codes: 0 = clean success, 1 = unrecoverable failure, 2 = self-healed and succeeded

app_dir := "CostSharingApp"
test_proj := app_dir / "tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj"
app_proj := app_dir / "src/CostSharingApp/CostSharingApp.csproj"
evidence_dir := "scratch/evidence"

# Emulator lifecycle
avd_name       := "cost-sharing-test"
android_home   := env("ANDROID_HOME", env("HOME") / "Library/Android/sdk")
emulator_bin   := android_home / "emulator/emulator"
sdkmanager_bin := android_home / "cmdline-tools/latest/bin/sdkmanager"
avdmanager_bin := android_home / "cmdline-tools/latest/bin/avdmanager"
app_package    := "com.costsharingapp.mobile"
sys_image      := "system-images;android-35;google_apis;arm64-v8a"

# List available commands
default:
    @just --list

# Restore NuGet dependencies
restore:
    cd {{app_dir}} && dotnet restore

# Build the test project (fast health check)
build:
    cd {{app_dir}} && dotnet build tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj

# Run all tests
test:
    dotnet test {{test_proj}}

# Run tests with detailed output and trx log
test-log:
    mkdir -p {{evidence_dir}}
    dotnet test {{test_proj}} --logger "trx;LogFileName={{justfile_directory()}}/{{evidence_dir}}/test-results.trx" -v normal

# Build the full MAUI app (Android)
build-app:
    dotnet build {{app_proj}} -f net9.0-android

# Clean build artifacts
clean:
    cd {{app_dir}} && dotnet clean

# Full health check with self-healing: restore + build + test (auto-recovers NuGet corruption)
check:
    #!/usr/bin/env bash
    set -uo pipefail
    healed=0

    # Try restore first
    set +e
    restore_out=$(cd {{app_dir}} && dotnet restore 2>&1)
    rc=$?
    set -e

    if [ $rc -ne 0 ]; then
        if echo "$restore_out" | grep -qiE "NU1301|NU1101|NU1403|corrupt|cache"; then
            echo "⚠️ Self-healed: NuGet cache issue detected — force-restoring"
            cd {{app_dir}} && dotnet restore --force-evaluate
            healed=1
        else
            echo "❌ Restore failed (unrecoverable):"
            echo "$restore_out"
            exit 1
        fi
    fi

    # Build
    set +e
    build_out=$(cd {{app_dir}} && dotnet build tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj 2>&1)
    rc=$?
    set -e

    if [ $rc -ne 0 ]; then
        if echo "$build_out" | grep -qiE "error MSB|error NETSDK|error CS|Could not copy|IOException|being used by another process|corrupt"; then
            echo "⚠️ Self-healed: Build failed — cleaning and rebuilding"
            cd {{app_dir}} && dotnet clean 2>/dev/null
            find {{app_dir}} -type d \( -name bin -o -name obj \) -exec rm -rf {} + 2>/dev/null || true
            cd {{app_dir}} && dotnet restore && dotnet build tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj
            healed=1
        else
            echo "❌ Build failed (unrecoverable):"
            echo "$build_out"
            exit 1
        fi
    fi

    # Test
    dotnet test {{test_proj}}

    if [ $healed -eq 1 ]; then
        echo "✅ Check passed (self-healed)"
        exit 2
    fi
    echo "✅ Check passed"

# Quick validate with self-healing: build + test (auto-recovers stale builds)
validate:
    #!/usr/bin/env bash
    set -uo pipefail
    healed=0

    set +e
    build_out=$(cd {{app_dir}} && dotnet build tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj 2>&1)
    rc=$?
    set -e

    if [ $rc -ne 0 ]; then
        # Only self-heal on known stale/incremental build patterns
        if echo "$build_out" | grep -qiE "error MSB|error NETSDK|error CS|Could not copy|IOException|being used by another process|corrupt"; then
            echo "⚠️ Self-healed: Stale build detected — cleaning and rebuilding"
            cd {{app_dir}} && dotnet clean 2>/dev/null
            find {{app_dir}} -type d \( -name bin -o -name obj \) -exec rm -rf {} + 2>/dev/null || true
            cd {{app_dir}} && dotnet build tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj
            healed=1
        else
            echo "❌ Build failed (unrecoverable):"
            echo "$build_out"
            exit 1
        fi
    fi

    dotnet test {{test_proj}}

    if [ $healed -eq 1 ]; then
        echo "✅ Validate passed (self-healed)"
        exit 2
    fi
    echo "✅ Validate passed"

# --- Emulator Lifecycle (L4 Self-Healing Harness) ---

# Create AVD if it doesn't exist; auto-installs missing system image
setup-emulator:
    #!/usr/bin/env bash
    set -euo pipefail
    healed=0

    if [ ! -x "{{emulator_bin}}" ]; then
        echo "❌ Emulator not found at {{emulator_bin}}"
        echo "   Set ANDROID_HOME or install Android SDK emulator package"
        exit 1
    fi

    if {{emulator_bin}} -list-avds 2>/dev/null | grep -q "{{avd_name}}"; then
        echo "✅ AVD '{{avd_name}}' already exists"
        exit 0
    fi

    # Auto-install system image if missing
    if [ ! -d "{{android_home}}/system-images/android-35/google_apis/arm64-v8a" ]; then
        if [ -x "{{sdkmanager_bin}}" ]; then
            echo "⚠️ Self-healed: System image missing — installing via sdkmanager"
            yes 2>/dev/null | "{{sdkmanager_bin}}" --licenses >/dev/null 2>&1 || true
            "{{sdkmanager_bin}}" "{{sys_image}}"
            healed=1
        else
            echo "❌ System image not found: {{sys_image}}"
            echo "   Fix: {{sdkmanager_bin}} '{{sys_image}}'"
            exit 1
        fi
    fi

    echo "Creating AVD '{{avd_name}}'..."
    echo "no" | "{{avdmanager_bin}}" create avd \
        --name "{{avd_name}}" \
        --package "{{sys_image}}" \
        --device "pixel_7" \
        --force
    echo "✅ AVD '{{avd_name}}' created"
    if [ $healed -eq 1 ]; then exit 2; fi

# Boot emulator (headless); auto-kills orphan emulators matching our AVD
boot-emulator:
    #!/usr/bin/env bash
    set -euo pipefail
    healed=0

    if ! command -v adb >/dev/null 2>&1; then
        echo "❌ ADB not found. Install Android SDK platform-tools and add to PATH."
        exit 1
    fi
    if [ ! -x "{{emulator_bin}}" ]; then
        echo "❌ Emulator not found at {{emulator_bin}}"
        echo "   Set ANDROID_HOME or install Android SDK emulator package"
        exit 1
    fi

    # Check for running emulators — accept ours, kill orphans matching our AVD
    if adb devices 2>/dev/null | grep -q "emulator"; then
        our_running=0
        for serial in $(adb devices 2>/dev/null | grep "emulator" | awk '{print $1}'); do
            set +e
            avd_check=$(adb -s "$serial" emu avd name 2>/dev/null | head -1 | tr -d '\r')
            set -e
            if [ "$avd_check" = "{{avd_name}}" ]; then
                echo "✅ Emulator already running (AVD: {{avd_name}}, serial: $serial)"
                our_running=1
            fi
        done
        if [ $our_running -eq 1 ]; then
            exit 0
        fi
        # No matching AVD found — kill orphans that don't match any known AVD
        for serial in $(adb devices 2>/dev/null | grep "emulator" | awk '{print $1}'); do
            set +e
            avd_check=$(adb -s "$serial" emu avd name 2>/dev/null | head -1 | tr -d '\r')
            set -e
            echo "⚠️ Self-healed: Orphan emulator detected (AVD: '$avd_check', serial: $serial) — killing"
            adb -s "$serial" emu kill 2>/dev/null || true
            healed=1
        done
        if [ $healed -eq 1 ]; then sleep 3; fi
    fi

    if ! {{emulator_bin}} -list-avds 2>/dev/null | grep -q "{{avd_name}}"; then
        echo "❌ AVD '{{avd_name}}' not found. Run 'just setup-emulator' first."
        exit 1
    fi

    echo "Booting emulator..."
    {{emulator_bin}} @{{avd_name}} -no-audio -no-boot-anim \
        -gpu swiftshader_indirect &
    adb wait-for-device
    elapsed=0
    while [ "$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" != "1" ]; do
        sleep 2
        elapsed=$((elapsed + 2))
        if [ $elapsed -ge 120 ]; then
            echo "❌ Emulator boot timed out after 120s"
            exit 1
        fi
    done
    adb shell input keyevent 82
    sleep 2
    echo "✅ Emulator ready (booted in ${elapsed}s)"
    if [ $healed -eq 1 ]; then exit 2; fi

# Build, deploy, and launch the app; retries once on crash
launch-app:
    #!/usr/bin/env bash
    set -euo pipefail
    healed=0

    if ! command -v adb >/dev/null 2>&1; then
        echo "❌ ADB not found. Install Android SDK platform-tools and add to PATH."
        exit 1
    fi
    if ! adb devices 2>/dev/null | grep -q "emulator"; then
        echo "❌ No emulator running. Run 'just boot-emulator' first."
        exit 1
    fi

    launch_once() {
        local launcher
        launcher=$(adb shell cmd package resolve-activity --brief \
            -c android.intent.category.LAUNCHER {{app_package}} 2>/dev/null | tail -1)
        adb shell am start -n "$launcher"
        sleep 3
        # Check if app process is alive
        if adb shell pidof {{app_package}} >/dev/null 2>&1; then
            return 0
        else
            return 1
        fi
    }

    echo "Building and deploying..."
    dotnet build {{app_proj}} -f net9.0-android -t:Install \
        -p:AndroidAttachDebugger=false
    echo "Launching app..."

    set +e
    launch_once
    rc=$?
    set -e

    if [ $rc -ne 0 ]; then
        echo "⚠️ Self-healed: App crashed on launch — uninstalling and redeploying"
        adb uninstall {{app_package}} 2>/dev/null || true
        dotnet build {{app_proj}} -f net9.0-android -t:Install \
            -p:AndroidAttachDebugger=false
        set +e
        launch_once
        rc=$?
        set -e
        if [ $rc -ne 0 ]; then
            echo "❌ App crashed again after clean deploy — unrecoverable"
            exit 1
        fi
        healed=1
    fi

    sleep 2
    if [ $healed -eq 1 ]; then
        echo "✅ App launched on emulator (self-healed)"
        exit 2
    fi
    echo "✅ App launched on emulator"

# Capture screenshot (optional label)
screenshot label="":
    #!/usr/bin/env bash
    set -euo pipefail
    if ! command -v adb >/dev/null 2>&1; then
        echo "❌ ADB not found. Install Android SDK platform-tools and add to PATH."
        exit 1
    fi
    mkdir -p {{evidence_dir}}
    ts=$(date +%Y%m%dT%H%M%S)
    if [ -n "{{label}}" ]; then
        safe_label=$(echo "{{label}}" | tr -cd '[:alnum:]-_')
        name="screenshot-${safe_label}-$ts.png"
    else
        name="screenshot-$ts.png"
    fi
    adb exec-out screencap -p > "{{evidence_dir}}/$name"
    echo "📸 Saved: {{evidence_dir}}/$name"

# ADB-scripted smoke test: launch app, navigate screens, capture evidence
smoke-test:
    #!/usr/bin/env bash
    set -euo pipefail
    if ! command -v adb >/dev/null 2>&1; then
        echo "❌ ADB not found. Install Android SDK platform-tools and add to PATH."
        exit 1
    fi
    mkdir -p {{evidence_dir}}
    ts=$(date +%Y%m%dT%H%M%S)

    if ! adb devices 2>/dev/null | grep -q "emulator"; then
        echo "❌ No emulator running. Run 'just boot-emulator' first."
        exit 1
    fi

    echo "=== Smoke Test: Step 1 — Launch app ==="
    launcher=$(adb shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER {{app_package}} | tail -1)
    adb shell am start -n "$launcher"
    sleep 5
    adb exec-out screencap -p > "{{evidence_dir}}/smoke-01-launch-$ts.png"
    echo "📸 smoke-01-launch captured"

    echo "=== Smoke Test: Step 2 — Tap first navigation item ==="
    adb shell input tap 540 2200
    sleep 3
    adb exec-out screencap -p > "{{evidence_dir}}/smoke-02-nav1-$ts.png"
    echo "📸 smoke-02-nav1 captured"

    echo "=== Smoke Test: Step 3 — Tap second navigation item ==="
    adb shell input tap 270 2200
    sleep 3
    adb exec-out screencap -p > "{{evidence_dir}}/smoke-03-nav2-$ts.png"
    echo "📸 smoke-03-nav2 captured"

    echo ""
    echo "✅ Smoke test complete — 3 screenshots in {{evidence_dir}}/"
    ls -la {{evidence_dir}}/smoke-*-$ts.png

# Graceful shutdown (saves quick-boot snapshot)
stop-emulator:
    #!/usr/bin/env bash
    set -euo pipefail
    if ! command -v adb >/dev/null 2>&1; then
        echo "❌ ADB not found. Install Android SDK platform-tools and add to PATH."
        exit 1
    fi
    if ! adb devices 2>/dev/null | grep -q "emulator"; then
        echo "✅ No emulator running"
        exit 0
    fi
    adb emu kill
    sleep 3
    echo "✅ Emulator stopped (snapshot saved)"

# --- Diagnostics (L4 Harness) ---

# Diagnose full environment and suggest fixes
doctor:
    #!/usr/bin/env bash
    set -uo pipefail
    issues=0
    echo "🩺 Harness Doctor — Environment Diagnostics"
    echo "============================================"
    echo ""

    # 1. .NET SDK
    if command -v dotnet >/dev/null 2>&1; then
        ver=$(dotnet --version 2>/dev/null)
        echo "✅ .NET SDK: $ver"
    else
        echo "❌ .NET SDK: not found"
        echo "   Fix: Install from https://dotnet.microsoft.com/download"
        issues=$((issues + 1))
    fi

    # 2. Android SDK
    if [ -d "{{android_home}}" ]; then
        echo "✅ Android SDK: {{android_home}}"
    else
        echo "❌ Android SDK: not found at {{android_home}}"
        echo "   Fix: Install Android Studio or set ANDROID_HOME"
        issues=$((issues + 1))
    fi

    # 3. Emulator binary
    if [ -x "{{emulator_bin}}" ]; then
        emu_ver=$("{{emulator_bin}}" -version 2>/dev/null | head -1 || echo "unknown")
        echo "✅ Emulator: $emu_ver"
    else
        echo "❌ Emulator: not found at {{emulator_bin}}"
        echo "   Fix: {{sdkmanager_bin}} 'emulator'"
        issues=$((issues + 1))
    fi

    # 4. ADB
    if command -v adb >/dev/null 2>&1; then
        adb_ver=$(adb version 2>/dev/null | head -1)
        echo "✅ ADB: $adb_ver"
    else
        echo "❌ ADB: not found"
        echo "   Fix: {{sdkmanager_bin}} 'platform-tools'"
        issues=$((issues + 1))
    fi

    # 5. System image
    if [ -d "{{android_home}}/system-images/android-35/google_apis/arm64-v8a" ]; then
        echo "✅ System image: {{sys_image}}"
    else
        echo "❌ System image: {{sys_image}} not installed"
        echo "   Fix: {{sdkmanager_bin}} '{{sys_image}}'"
        issues=$((issues + 1))
    fi

    # 6. AVD
    if [ -x "{{emulator_bin}}" ] && {{emulator_bin}} -list-avds 2>/dev/null | grep -q "{{avd_name}}"; then
        echo "✅ AVD: {{avd_name}}"
    else
        echo "❌ AVD: '{{avd_name}}' not found"
        echo "   Fix: just setup-emulator"
        issues=$((issues + 1))
    fi

    # 7. NuGet restore
    set +e
    restore_out=$(cd {{app_dir}} && dotnet restore --verbosity quiet 2>&1)
    rc=$?
    set -e
    if [ $rc -eq 0 ]; then
        echo "✅ NuGet restore: packages OK"
    else
        echo "❌ NuGet restore: failed"
        echo "   Fix: just check  (auto-heals NuGet issues)"
        issues=$((issues + 1))
    fi

    echo ""
    if [ $issues -eq 0 ]; then
        echo "🎉 All 7 checks passed — environment is healthy"
    else
        echo "⚠️  $issues issue(s) found — see fix commands above"
        exit 1
    fi

# AI code review sensor — inferential feedback on changed files (30s timeout, non-blocking)
review base="HEAD":
    #!/usr/bin/env bash
    set +e
    bash scripts/review.sh "{{base}}"
    rc=$?
    if [ $rc -eq 1 ]; then
        echo "⚠️ Review found critical issues (non-blocking in v1)"
        exit 0
    fi
    exit 0

harness task:
    bash scripts/harness.sh "{{task}}"