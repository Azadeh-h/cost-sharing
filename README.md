# Cost Sharing App

A cross-platform mobile application for tracking shared expenses and settling debts among groups, built with .NET MAUI.

## Features

✅ **Group Management** - Create and manage cost-sharing groups  
✅ **Expense Tracking** - Add expenses with even or custom splits  
✅ **Debt Simplification** - Min-Cash-Flow algorithm reduces transactions  
✅ **Settlement Recording** - Track payments and update balances automatically  
✅ **Personal Dashboard** - View total balance across all groups  
✅ **Transaction History** - Filter and view all expenses with date/type filters  
✅ **Offline Support** - Local SQLite storage for all data  

## Architecture

- **.NET MAUI** - Cross-platform UI framework
- **MVVM Pattern** - Separation of concerns with ViewModels
- **SQLite** - Local data storage
- **Dependency Injection** - Service-based architecture

## Project Structure

```
CostSharingApp/
├── src/
│   ├── CostSharing.Core/          # Core business logic & models
│   │   ├── Models/                 # Domain models (Group, Expense, Settlement, etc.)
│   │   ├── Interfaces/             # Service interfaces
│   │   ├── Services/               # Business services
│   │   └── Algorithms/             # Debt simplification algorithm
│   └── CostSharingApp/             # MAUI application
│       ├── Views/                  # XAML pages
│       ├── ViewModels/             # ViewModels
│       ├── Services/               # App services (Auth, Expense, Group, etc.)
│       ├── Converters/             # Value converters
│       └── Resources/              # Images, styles, colors
├── tests/
│   └── CostSharingApp.Tests/      # Unit tests (40 tests)
└── specs/                          # Requirements and task documentation
```

## Prerequisites

### All Platforms
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022 (Windows) or Visual Studio Code (Mac/Linux)

### iOS Development
- **macOS** with Xcode 26.0 or later
- iOS SDK
- Apple Developer account (for device deployment)

### Android Development
- Android SDK (API 35 or later)
- Android Emulator or physical device

### macOS Development
- macOS with Xcode 26.0 or later
- Mac Catalyst support

### Windows Development
- Windows 10/11 (Build 19041 or later)
- Windows SDK

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/cost-sharing.git
cd cost-sharing/CostSharingApp
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build & Run

#### Run Tests

```bash
cd CostSharingApp
dotnet test tests/CostSharingApp.Tests/CostSharingApp.Tests.csproj
```

All 40 unit tests should pass.

#### iOS Simulator

```bash
dotnet build src/CostSharingApp/CostSharingApp.csproj -f net9.0-ios -t:Run -p:RuntimeIdentifier=iossimulator-arm64
```

Or open in Visual Studio for Mac and select iOS Simulator target.

#### Android Emulator

```bash
dotnet build src/CostSharingApp/CostSharingApp.csproj -f net9.0-android -t:Run
```

Or open in Visual Studio and select Android Emulator target.

#### Mac Catalyst

```bash
dotnet build src/CostSharingApp/CostSharingApp.csproj -f net9.0-maccatalyst -t:Run
```

#### Windows

```bash
dotnet build src/CostSharingApp/CostSharingApp.csproj -f net9.0-windows10.0.19041.0 -t:Run
```

Or open in Visual Studio 2022 on Windows.

## Development

### Code Style

- C# coding conventions (StyleCop rules)
- XML documentation for all public APIs
- MVVM pattern throughout

### Adding a New Feature

1. Create models in `CostSharing.Core/Models/`
2. Define interfaces in `CostSharing.Core/Interfaces/`
3. Implement services in `CostSharingApp/Services/`
4. Create ViewModels in `CostSharingApp/ViewModels/`
5. Design XAML views in `CostSharingApp/Views/`
6. Register services and pages in `MauiProgram.cs`
7. Add unit tests in `CostSharingApp.Tests/`

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "FullyQualifiedName~SplitCalculationServiceTests"
```

## Troubleshooting

### Xcode Version Mismatch

If you see "This version requires Xcode 26.0":

```bash
# Check current Xcode version
xcodebuild -version

# Set Xcode path (if multiple versions installed)
sudo xcode-select --switch /Applications/Xcode26.0.app
```

Or update .NET MAUI workload:

```bash
dotnet workload update
```

### Android SDK Not Found

```bash
# Install Android SDK via Visual Studio Installer or:
dotnet build -t:InstallAndroidDependencies
```

Set `ANDROID_HOME` environment variable to SDK location.



## Testing

### Unit Test Coverage

- **Split Calculation**: 16 tests (even splits, custom splits, edge cases)
- **Debt Calculation**: 13 tests (settlements, aggregation, multi-expense)
- **Debt Simplification**: 11 tests (Min-Cash-Flow algorithm optimization)

**Total: 40 tests, 100% passing**

### Manual Testing Checklist

- [ ] Create group and invite members
- [ ] Add expenses with even/custom split
- [ ] View simplified debts (Min-Cash-Flow)
- [ ] Record settlement
- [ ] View dashboard with multiple groups
- [ ] Filter transaction history
- [ ] Pull-to-refresh updates data
- [ ] Offline mode shows cached data

## Deployment

### iOS App Store

1. Archive in Xcode (Product > Archive)
2. Sign with distribution certificate
3. Upload to App Store Connect
4. Submit for review

### Google Play Store

**Generate Keystore** (first time only):
```bash
keytool -genkey -v -keystore costsharingapp.keystore \
  -alias costsharingapp -keyalg RSA -keysize 2048 -validity 10000
```

**Set Environment Variables** (for automated builds):
```bash
export AndroidKeyPassword="your_key_password"
export AndroidStorePassword="your_store_password"
```

**Build Release APK**:
```bash
dotnet publish -f net9.0-android -c Release
```

The signed APK will be in `bin/Release/net9.0-android/publish/`.

**Upload to Play Console**:
1. Sign in to [Google Play Console](https://play.google.com/console)
2. Create new app and fill in store listing
3. Upload APK/AAB to internal testing track
4. Complete content rating questionnaire
5. Set pricing and distribution
6. Submit for review

### Microsoft Store (Windows)

**Build MSIX Package**:
```bash
dotnet publish -f net9.0-windows10.0.19041.0 -c Release
```

The MSIX package will be in `bin/Release/net9.0-windows10.0.19041.0/publish/`.

**Upload to Partner Center**:
1. Sign in to [Microsoft Partner Center](https://partner.microsoft.com/dashboard)
2. Create new app submission
3. Upload MSIX package
4. Complete store listing with screenshots
5. Set pricing and availability
6. Submit for certification

**Note**: You may need to update the `Publisher` in `Package.appxmanifest` to match your Partner Center publisher identity.

## iOS Provisioning Setup

1. **Create App ID** in Apple Developer Portal:
   - Bundle ID: `com.costsharingapp.mobile`
   - Enable capabilities: Push Notifications, Background Modes

2. **Generate Provisioning Profile**:
   - Type: App Store Distribution
   - Select the App ID created above
   - Select distribution certificate
   - Download and install in Xcode

3. **Update Xcode Settings**:
   - Open project in Xcode
   - Select target > Signing & Capabilities
   - Choose provisioning profile: "Cost Sharing App Store"

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

[Your License Here]

## Contact

[Your Contact Information]

## Acknowledgments

- Min-Cash-Flow Algorithm for debt simplification
- .NET MAUI team for the cross-platform framework
