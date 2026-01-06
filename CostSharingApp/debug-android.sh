#!/bin/bash
# Android Debug Script for Cost Sharing App

# Set up ADB path
export PATH=$PATH:/opt/homebrew/share/android-commandlinetools/platform-tools

echo "=== Cost Sharing App - Android Debug Tool ==="
echo ""

# Check device connection
echo "Checking connected devices..."
adb devices
echo ""

# Prompt for action
echo "Select an option:"
echo "1) Install APK"
echo "2) View live logs (filtered)"
echo "3) View all error logs"
echo "4) Clear logs and restart app"
echo "5) Uninstall app"
echo ""
read -p "Enter choice (1-5): " choice

case $choice in
    1)
        echo "Installing APK..."
        adb install -r src/CostSharingApp/bin/Debug/net9.0-android/com.costsharingapp.mobile-Signed.apk
        ;;
    2)
        echo "Viewing live logs (Ctrl+C to stop)..."
        adb logcat | grep -E "costsharingapp|CostSharing|mono|FATAL|AndroidRuntime"
        ;;
    3)
        echo "Viewing error logs..."
        adb logcat *:E | grep -E "costsharingapp|CostSharing|mono"
        ;;
    4)
        echo "Clearing logs..."
        adb logcat -c
        echo "Starting app..."
        adb shell am start -n com.costsharingapp.mobile/crc64e205aeaee53652b8.MainActivity
        echo "Monitoring logs (Ctrl+C to stop)..."
        sleep 2
        adb logcat | grep -E "costsharingapp|CostSharing|mono|FATAL|AndroidRuntime|System.err"
        ;;
    5)
        echo "Uninstalling app..."
        adb uninstall com.costsharingapp.mobile
        ;;
    *)
        echo "Invalid choice"
        ;;
esac
