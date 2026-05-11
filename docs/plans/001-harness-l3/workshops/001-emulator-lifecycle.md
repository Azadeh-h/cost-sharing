# Workshop: Emulator Lifecycle

**Type**: CLI Flow
**Plan**: 001-harness-l3
**Spec**: [harness-l3-spec.md](../harness-l3-spec.md)
**Created**: 2026-05-11
**Status**: Draft

**Related Documents**:
- `justfile` — existing build/test recipes (will be extended)
- `docs/project-rules/harness.md` — L2 harness spec (target: L3)

---

## Purpose

Define the complete emulator lifecycle — from environment setup through boot, app deploy, screenshot capture, and shutdown — as concrete `just` recipes. This workshop resolves whether to use cold boot vs quick boot, persistent vs ephemeral AVDs, and exactly what ADB commands each recipe runs.

## Key Questions Addressed

- Quick boot vs cold boot? Persistent vs ephemeral AVD?
- What happens if no AVD exists? Auto-create or error?
- How do we detect "emulator is ready" reliably?
- What's the expected boot time budget (cold vs warm)?
- How do shutdown/cleanup work?

---

## Current State (Machine Inventory)

| Component | Status | Location |
|-----------|--------|----------|
| Android SDK | ✅ Installed | `~/Library/Android/sdk/` |
| `ANDROID_HOME` | ❌ **Not set** | Needs export |
| `emulator` | ✅ Installed | `~/Library/Android/sdk/emulator/emulator` |
| `adb` | ✅ Installed | `/opt/homebrew/bin/adb` + SDK copy |
| `avdmanager` | ✅ Installed | `/opt/homebrew/bin/avdmanager` + SDK copy |
| `sdkmanager` | ✅ Installed | `/opt/homebrew/bin/sdkmanager` |
| AVDs | ❌ **None exist** | Must create one |
| System images | ✅ `android-35` installed | `~/Library/Android/sdk/system-images/android-35/` |
| ADB version | ✅ 1.0.41 / 36.0.2 | Working, no devices attached |

**App Config** (from `.csproj`):
- Target: `net9.0-android`
- Min SDK: `21.0`
- App ID: `com.costsharingapp.mobile`

---

## Environment Setup

Before any emulator commands work, `ANDROID_HOME` must be set.

```bash
# Required — add to shell profile (.zshrc / .bashrc)
export ANDROID_HOME="$HOME/Library/Android/sdk"
export PATH="$ANDROID_HOME/emulator:$ANDROID_HOME/platform-tools:$PATH"
```

**Why this matters**: The `emulator` binary at `$ANDROID_HOME/emulator/emulator` is the canonical one. Homebrew's `avdmanager`/`adb` work but reference the SDK path. Without `ANDROID_HOME`, `emulator` and `avdmanager` may fail silently or look in the wrong location.

The justfile will set `ANDROID_HOME` as a variable so recipes work regardless of shell profile.

---

## AVD Strategy: Persistent, Named AVD

**Decision**: Use a single **persistent, named AVD** called `cost-sharing-test`.

**Why persistent (not ephemeral)**:
- Quick boot from snapshot: ~10s vs ~45s cold boot
- Consistent screen size and density across runs
- No teardown/rebuild overhead
- Snapshots survive reboots

**Why a named AVD (not anonymous)**:
- Recipes can target it by name
- Multiple developers get the same config
- Easy to delete and recreate if corrupted

### AVD Specification

| Setting | Value | Rationale |
|---------|-------|-----------|
| Name | `cost-sharing-test` | Descriptive, project-scoped |
| Device | `pixel_7` | Modern screen size, well-supported skin |
| System image | `system-images;android-35;google_apis;x86_64` | Matches installed SDK, Google APIs for OAuth |
| RAM | 2048 MB | Sufficient for MAUI apps |
| Internal storage | 2048 MB | Room for app + data |
| SD card | none | Not needed |
| GPU | `auto` | Hardware accel on macOS (HVF) |

### Create AVD

```
$ avdmanager create avd \
    --name cost-sharing-test \
    --package "system-images;android-35;google_apis;x86_64" \
    --device pixel_7 \
    --force

┌─────────────────────────────────────────────────────────────┐
│ CREATE AVD                                                  │
│   Name:    cost-sharing-test                                │
│   Image:   android-35 / google_apis / x86_64               │
│   Device:  pixel_7                                          │
│   --force: Overwrite if exists                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ OUTPUT                                                      │
│                                                             │
│   Auto-selecting single ABI x86_64                         │
│   Do you wish to create a custom hardware profile [no]     │
│   → no                                                      │
│   AVD 'cost-sharing-test' created successfully.            │
│                                                             │
│   Files: ~/.android/avd/cost-sharing-test.avd/             │
└─────────────────────────────────────────────────────────────┘
```

---

## Emulator Boot Flow

### Boot Strategy: Quick Boot (with Cold Boot Fallback)

```
┌──────────────────────┐
│ just boot-emulator   │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────────────────────────────────────┐
│ 1. CHECK: Is emulator already running?               │
│    adb devices | grep emulator                       │
│    If yes → skip boot, report "already running"      │
└──────────┬───────────────────────────────────────────┘
           │ no
           ▼
┌──────────────────────────────────────────────────────┐
│ 2. CHECK: Does AVD 'cost-sharing-test' exist?        │
│    emulator -list-avds | grep cost-sharing-test      │
│    If no → ERROR with setup instructions             │
└──────────┬───────────────────────────────────────────┘
           │ yes
           ▼
┌──────────────────────────────────────────────────────┐
│ 3. BOOT: Start emulator in background                │
│    emulator @cost-sharing-test                       │
│      -no-window           (headless for agents)      │
│      -no-audio                                       │
│      -no-boot-anim                                   │
│      -gpu swiftshader_indirect  (safe GPU)           │
│      &                                               │
└──────────┬───────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────┐
│ 4. WAIT: Poll until boot complete                    │
│    Loop (max 120s, poll every 2s):                   │
│      adb wait-for-device                             │
│      adb shell getprop sys.boot_completed            │
│      → "1" means ready                              │
│    On timeout → kill emulator, ERROR                 │
└──────────┬───────────────────────────────────────────┘
           │ boot_completed == 1
           ▼
┌──────────────────────────────────────────────────────┐
│ 5. STABILIZE: Extra wait for launcher                │
│    adb shell input keyevent 82   (unlock screen)     │
│    sleep 2                       (settle)            │
└──────────┬───────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────┐
│ OUTPUT                                               │
│   ✅ Emulator ready (booted in 38s)                  │
│   Device: emulator-5554                              │
└──────────────────────────────────────────────────────┘
```

### Boot Modes Comparison

| Mode | Flag | Boot Time | When to Use |
|------|------|-----------|-------------|
| Quick boot (default) | _(none)_ | ~10-15s | Normal workflow — resumes from snapshot |
| Cold boot | `-no-snapshot-load` | ~40-60s | After AVD creation, debugging boot issues |
| Headless | `-no-window` | Same | Agent/CI use — no GUI needed |
| Windowed | _(omit `-no-window`)_ | Same | Human debugging — see the screen |

**Default for agents**: Quick boot + headless. Saves ~30s per boot cycle.

**Why `swiftshader_indirect` GPU**: Hardware GPU (`host`) is faster but can crash in headless mode on some macOS configurations. `swiftshader_indirect` is slower but reliable. Can be upgraded to `host` if stable.

---

## App Deployment & Launch

```
┌────────────────────┐
│ just launch-app    │
└────────┬───────────┘
         │
         ▼
┌──────────────────────────────────────────────────────┐
│ 1. CHECK: Is emulator running?                       │
│    adb devices | grep emulator                       │
│    If no → ERROR "Run 'just boot-emulator' first"   │
└────────┬─────────────────────────────────────────────┘
         │ yes
         ▼
┌──────────────────────────────────────────────────────┐
│ 2. BUILD: Build APK for Android                      │
│    dotnet build                                      │
│      CostSharingApp/src/CostSharingApp/              │
│        CostSharingApp.csproj                         │
│      -f net9.0-android                               │
│      -t:Install                                      │
│      -p:AndroidAttachDebugger=false                  │
│                                                      │
│    → Builds AND installs APK to emulator             │
└────────┬─────────────────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────────────────┐
│ 3. LAUNCH: Start the app                             │
│    adb shell am start -n                             │
│      com.costsharingapp.mobile/                      │
│      crc64...MainActivity                            │
│                                                      │
│    OR: adb shell monkey -p                           │
│      com.costsharingapp.mobile                       │
│      -c android.intent.category.LAUNCHER 1           │
│    (monkey is simpler — doesn't need activity hash)  │
└────────┬─────────────────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────────────────┐
│ 4. WAIT: App is rendering                            │
│    sleep 5   (MAUI cold start can take 3-5s)         │
└────────┬─────────────────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────────────────┐
│ OUTPUT                                               │
│   ✅ App launched on emulator-5554                   │
│   Package: com.costsharingapp.mobile                 │
└──────────────────────────────────────────────────────┘
```

**Why `monkey` over `am start`**: The `monkey` command just needs the package name. `am start` needs the fully-qualified activity name, which in MAUI includes a `crc64` hash that changes across builds. `monkey` is more robust.

**Why `-t:Install`**: The `dotnet build -t:Install` target builds the APK AND deploys it to the connected device in one step. No separate `adb install` needed.

---

## Screenshot Capture

```bash
$ just screenshot
# or
$ just screenshot "after-login"
```

```
┌─────────────────────────────────────────────────────────┐
│ 1. CAPTURE: Take screenshot via ADB                     │
│    adb exec-out screencap -p > scratch/evidence/        │
│      screenshot-{timestamp}.png                         │
│                                                         │
│    If label provided:                                   │
│      screenshot-{label}-{timestamp}.png                 │
└─────────┬───────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────┐
│ OUTPUT                                                  │
│   📸 Screenshot saved: scratch/evidence/                │
│      screenshot-after-login-20260511T143600.png         │
│   Size: 1080x2400                                       │
└─────────────────────────────────────────────────────────┘
```

**Why `exec-out screencap -p`**: Pipes PNG directly to host — no temp file on device, no `adb pull` step. Single command, fast.

**Evidence directory**: `scratch/evidence/` is gitignored, so screenshots don't pollute the repo but persist across test runs for agent review.

---

## Shutdown

```
┌──────────────────────┐
│ just stop-emulator   │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────────────────────────────────────┐
│ 1. SAVE SNAPSHOT: For quick boot next time           │
│    adb emu kill                                      │
│    (emulator auto-saves snapshot on graceful kill)    │
└──────────┬───────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────┐
│ 2. VERIFY: Emulator process gone                     │
│    Wait up to 10s for process to exit                │
│    adb devices → should show empty list              │
└──────────┬───────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────┐
│ OUTPUT                                               │
│   ✅ Emulator stopped (snapshot saved)               │
└──────────────────────────────────────────────────────┘
```

**Why `adb emu kill`** (not `kill -9`): Graceful shutdown saves quick-boot snapshot automatically. Next boot resumes in ~10s instead of ~45s cold boot.

---

## Complete Lifecycle (Happy Path)

```bash
# One-time setup
just setup-emulator          # Create AVD if not exists

# Per-session workflow
just boot-emulator           # ~10s (quick boot) or ~45s (cold)
just launch-app              # Build + deploy + launch (~60s)
just screenshot "home"       # Capture current state
just stop-emulator           # Save snapshot + shutdown
```

```
  setup-emulator ──────► boot-emulator ──────► launch-app
  (one-time)              (per session)         (per change)
                               │                     │
                               │                     ▼
                               │               screenshot ◄──── (repeat)
                               │                     │
                               ▼                     │
                          stop-emulator ◄────────────┘
                          (end of session)
```

---

## Justfile Recipe Sketches

```just
# --- Emulator lifecycle ---

avd_name   := "cost-sharing-test"
android_home := env("ANDROID_HOME", env("HOME") / "Library/Android/sdk")
emulator   := android_home / "emulator/emulator"
evidence   := "scratch/evidence"

# Create AVD if it doesn't exist (one-time setup)
setup-emulator:
    @if {{emulator}} -list-avds | grep -q "{{avd_name}}"; then \
        echo "✅ AVD '{{avd_name}}' already exists"; \
    else \
        echo "Creating AVD '{{avd_name}}'..."; \
        avdmanager create avd \
            --name {{avd_name}} \
            --package "system-images;android-35;google_apis;x86_64" \
            --device pixel_7 \
            --force; \
        echo "✅ AVD '{{avd_name}}' created"; \
    fi

# Boot emulator (headless, quick boot)
boot-emulator:
    @if adb devices | grep -q emulator; then \
        echo "✅ Emulator already running"; exit 0; \
    fi
    @echo "Booting emulator..."
    @{{emulator}} @{{avd_name}} -no-window -no-audio -no-boot-anim \
        -gpu swiftshader_indirect &
    @adb wait-for-device
    @timeout=120; elapsed=0; \
    while [ "$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" != "1" ]; do \
        sleep 2; elapsed=$((elapsed + 2)); \
        if [ $elapsed -ge $timeout ]; then \
            echo "❌ Emulator boot timed out after ${timeout}s"; exit 1; \
        fi; \
    done
    @adb shell input keyevent 82
    @sleep 2
    @echo "✅ Emulator ready"

# Build, deploy, and launch the app
launch-app:
    @if ! adb devices | grep -q emulator; then \
        echo "❌ No emulator running. Run 'just boot-emulator' first"; exit 1; \
    fi
    dotnet build CostSharingApp/src/CostSharingApp/CostSharingApp.csproj \
        -f net9.0-android -t:Install -p:AndroidAttachDebugger=false
    @adb shell monkey -p com.costsharingapp.mobile \
        -c android.intent.category.LAUNCHER 1
    @sleep 5
    @echo "✅ App launched"

# Capture screenshot (optional label argument)
screenshot label="":
    @mkdir -p {{evidence}}
    @ts=$(date +%Y%m%dT%H%M%S); \
    if [ -n "{{label}}" ]; then \
        name="screenshot-{{label}}-$ts.png"; \
    else \
        name="screenshot-$ts.png"; \
    fi; \
    adb exec-out screencap -p > "{{evidence}}/$name"; \
    echo "📸 Saved: {{evidence}}/$name"

# Graceful shutdown (saves quick-boot snapshot)
stop-emulator:
    @if ! adb devices | grep -q emulator; then \
        echo "✅ No emulator running"; exit 0; \
    fi
    @adb emu kill
    @sleep 3
    @echo "✅ Emulator stopped (snapshot saved)"
```

---

## Error Scenarios

| Scenario | Detection | Recipe Behavior |
|----------|-----------|-----------------|
| No AVD exists | `emulator -list-avds` empty | `boot-emulator` → ERROR with "Run `just setup-emulator`" |
| `ANDROID_HOME` not set | Variable empty | Justfile falls back to `~/Library/Android/sdk` |
| Emulator boot timeout | 120s elapsed, `sys.boot_completed != 1` | Kill emulator process, exit 1 |
| No system image installed | `avdmanager create` fails | `setup-emulator` → ERROR with `sdkmanager install` instructions |
| Emulator already running | `adb devices` shows emulator | `boot-emulator` → skip, report already running |
| No emulator when launching app | `adb devices` empty | `launch-app` → ERROR with "Run `just boot-emulator`" |
| App not installed | `monkey` returns error | `launch-app` builds first, so this shouldn't happen |
| ADB not found | `which adb` fails | Justfile recipe fails immediately with clear error |

---

## Timing Budget

| Operation | Expected Time | Notes |
|-----------|--------------|-------|
| Quick boot (snapshot resume) | 10-15s | After first cold boot |
| Cold boot (no snapshot) | 40-60s | First boot or after `setup-emulator` |
| App build + deploy (`-t:Install`) | 30-60s | Incremental builds faster |
| App launch (monkey) | 3-5s | MAUI cold start |
| Screenshot capture | <1s | Direct pipe via `exec-out` |
| Graceful shutdown | 2-3s | Saves snapshot |
| **Full cycle (warm)** | **~50-80s** | Boot + build + deploy + launch |
| **Full cycle (cold)** | **~80-120s** | First time only |

**90-second acceptance target**: Achievable with quick boot. Cold boot may exceed on first run — this is acceptable since it only happens once.

---

## Open Questions

### Q1: Should `boot-emulator` use headless (`-no-window`) by default?

**RESOLVED**: Yes, headless by default. Agents don't need a GUI. Add a `boot-emulator-gui` recipe for human debugging:
```just
boot-emulator-gui:
    {{emulator}} @{{avd_name}} -no-audio -no-boot-anim -gpu host &
    # ... same wait logic ...
```

### Q2: Should `setup-emulator` auto-install the system image if missing?

**OPEN**: Two options:
- **Option A**: Auto-install via `sdkmanager "system-images;android-35;google_apis;x86_64"` — fully automated but downloads ~1GB
- **Option B**: Error with instructions — safer, lets user choose image
- **Recommendation**: Option B for now. Image download is a one-time heavy operation that should be explicit.

### Q3: What if multiple emulators are running?

**RESOLVED**: Recipes target the first `emulator-*` device from `adb devices`. For this project, only one emulator is expected. Multi-device support is a non-goal (per spec).

---

## Quick Reference

```bash
# One-time setup
just setup-emulator              # Create AVD

# Daily workflow
just boot-emulator               # Start (headless, quick boot)
just launch-app                  # Build + deploy + open app
just screenshot "label"          # Capture screen
just stop-emulator               # Graceful shutdown

# Troubleshooting
just boot-emulator-gui           # Start with visible window
adb logcat -d | tail -50         # Recent device logs
adb shell dumpsys activity       # Running activities
emulator -list-avds              # List available AVDs
```
