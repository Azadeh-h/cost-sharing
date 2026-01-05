# Docker Build Environment

This directory contains Docker configuration for building the CostSharing MAUI app in a containerized environment.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose installed

## Quick Start

### Build All Platforms

```bash
cd CostSharingApp
docker-compose up -d
./build/docker-build.sh all
```

### Build Specific Platform

```bash
# Android only
./build/docker-build.sh android

# Windows only
./build/docker-build.sh windows
```

## Interactive Development

Enter the container for manual builds:

```bash
docker-compose run --rm maui-builder bash
```

Inside the container:

```bash
# Restore dependencies
dotnet restore

# Build for Android
dotnet build src/CostSharingApp/CostSharingApp.csproj -f net9.0-android -c Release

# Publish Android APK
dotnet publish src/CostSharingApp/CostSharingApp.csproj -f net9.0-android -c Release -o ./output/android

# Run tests
dotnet test tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj
```

## Output Location

Built installers will be in `./output/`:
- `./output/android/` - Android APK files
- `./output/windows/` - Windows MSIX/exe files

## Cleanup

```bash
# Stop container
docker-compose down

# Remove container and volumes
docker-compose down -v

# Remove built images
docker rmi costsharing-builder
```

## Notes

- **macOS/iOS builds**: Docker cannot build iOS/.dmg (requires macOS host with Xcode). Use native tooling for those platforms.
- **Container purpose**: This is a BUILD environment only, not for running the app
- **Volume mounts**: Source code is mounted, so changes are reflected immediately
- **NuGet cache**: Packages are cached in a Docker volume for faster rebuilds

## CI/CD Integration

For GitHub Actions or other CI systems:

```yaml
- name: Build with Docker
  run: |
    docker-compose up -d
    docker-compose exec -T maui-builder dotnet publish src/CostSharingApp/CostSharingApp.csproj -f net9.0-android -c Release -o ./output/android
```
