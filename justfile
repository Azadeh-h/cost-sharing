# Cost-Sharing App — Agent Harness Commands
# See docs/project-rules/harness.md for full harness documentation

app_dir := "CostSharingApp"
test_proj := app_dir / "tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj"
app_proj := app_dir / "src/CostSharingApp/CostSharingApp.csproj"
evidence_dir := "scratch/evidence"

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
