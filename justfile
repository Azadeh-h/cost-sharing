# Cost-Sharing App — Agent Harness Commands
# See docs/project-rules/harness.md for full harness documentation

app_dir := "CostSharingApp"
test_proj := app_dir / "tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj"
app_proj := app_dir / "src/CostSharingApp/CostSharingApp.csproj"
evidence_dir := "scratch/evidence"

# Emulator lifecycle
avd_name     := "cost-sharing-test"
android_home := env("ANDROID_HOME", env("HOME") / "Library/Android/sdk")
emulator_bin := android_home / "emulator/emulator"
app_package  := "com.costsharingapp.mobile"
sys_image    := "system-images;android-35;google_apis;arm64-v8a"

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

# Full health check: restore + build + test
check: restore build test

# Quick validate: build + test (skip restore)
validate: build test

# --- Emulator Lifecycle (L3 Harness) ---

# Create AVD if it doesn't exist (one-time setup)
setup-emulator:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -x "{{emulator_bin}}" ]; then
        echo "❌ Emulator not found at {{emulator_bin}}"
        echo "   Set ANDROID_HOME or install Android SDK emulator package"
        exit 1
    fi
    if {{emulator_bin}} -list-avds 2>/dev/null | grep -q "{{avd_name}}"; then
        echo "✅ AVD '{{avd_name}}' already exists"
    else
        echo "Creating AVD '{{avd_name}}'..."
        if [ ! -d "{{android_home}}/system-images/android-35/google_apis/arm64-v8a" ]; then
            echo "❌ System image not found: {{sys_image}}"
            echo "   Install with: sdkmanager '{{sys_image}}'"
            exit 1
        fi
        echo "no" | avdmanager create avd \
            --name "{{avd_name}}" \
            --package "{{sys_image}}" \
            --device "pixel_7" \
            --force
        echo "✅ AVD '{{avd_name}}' created"
    fi

# Boot emulator (headless, quick boot)
boot-emulator:
    #!/usr/bin/env bash
    set -euo pipefail
    if ! command -v adb >/dev/null 2>&1; then
        echo "❌ ADB not found. Install Android SDK platform-tools and add to PATH."
        exit 1
    fi
    if [ ! -x "{{emulator_bin}}" ]; then
        echo "❌ Emulator not found at {{emulator_bin}}"
        echo "   Set ANDROID_HOME or install Android SDK emulator package"
        exit 1
    fi
    if adb devices 2>/dev/null | grep -q "emulator"; then
        echo "✅ Emulator already running"
        exit 0
    fi
    if ! {{emulator_bin}} -list-avds 2>/dev/null | grep -q "{{avd_name}}"; then
        echo "❌ AVD '{{avd_name}}' not found. Run 'just setup-emulator' first."
        exit 1
    fi
    echo "Booting emulator..."
    {{emulator_bin}} @{{avd_name}} -no-window -no-audio -no-boot-anim \
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

# Build, deploy, and launch the app on emulator
launch-app:
    #!/usr/bin/env bash
    set -euo pipefail
    if ! command -v adb >/dev/null 2>&1; then
        echo "❌ ADB not found. Install Android SDK platform-tools and add to PATH."
        exit 1
    fi
    if ! adb devices 2>/dev/null | grep -q "emulator"; then
        echo "❌ No emulator running. Run 'just boot-emulator' first."
        exit 1
    fi
    echo "Building and deploying..."
    dotnet build {{app_proj}} -f net9.0-android -t:Install \
        -p:AndroidAttachDebugger=false
    echo "Launching app..."
    # Discover the LAUNCHER activity and start it directly (monkey is unreliable)
    launcher=$(adb shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER {{app_package}} | tail -1)
    adb shell am start -n "$launcher"
    sleep 5
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
