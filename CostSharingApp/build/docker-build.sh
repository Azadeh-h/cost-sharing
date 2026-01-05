#!/bin/bash
# Docker-based build script for CostSharing App
# Usage: ./docker-build.sh [android|windows|all]

set -e

PLATFORM=${1:-all}
OUTPUT_DIR="./output"

echo "🐳 Building CostSharing App in Docker container..."

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

# Start the builder container
docker-compose up -d maui-builder

build_android() {
    echo "📱 Building Android APK..."
    docker-compose exec maui-builder dotnet publish src/CostSharingApp/CostSharingApp.csproj \
        -f net9.0-android \
        -c Release \
        -o ./output/android \
        -p:AndroidPackageFormat=apk
    echo "✅ Android APK built: $OUTPUT_DIR/android/"
}

build_windows() {
    echo "🪟 Building Windows installer..."
    docker-compose exec maui-builder dotnet publish src/CostSharingApp/CostSharingApp.csproj \
        -f net9.0-windows10.0.19041.0 \
        -c Release \
        -o ./output/windows
    echo "✅ Windows build completed: $OUTPUT_DIR/windows/"
}

case "$PLATFORM" in
    android)
        build_android
        ;;
    windows)
        build_windows
        ;;
    all)
        build_android
        build_windows
        ;;
    *)
        echo "❌ Unknown platform: $PLATFORM"
        echo "Usage: $0 [android|windows|all]"
        exit 1
        ;;
esac

echo "🎉 Build complete! Installers are in: $OUTPUT_DIR/"

# Keep container running for potential additional builds
echo "💡 Container is still running. To stop: docker-compose down"
