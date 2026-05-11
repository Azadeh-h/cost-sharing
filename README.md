# Cost Sharing App

A mobile application for tracking shared expenses and settling debts among groups, built with .NET MAUI for Android.

## Features

✅ **Group Management** - Create and manage cost-sharing groups  
✅ **Expense Tracking** - Add expenses with even or custom splits  
✅ **Debt Simplification** - Min-Cash-Flow algorithm reduces transactions  
✅ **Settlement Recording** - Track payments and update balances automatically  
✅ **Personal Dashboard** - View total balance across all groups  
✅ **Transaction History** - Filter and view all expenses with date/type filters  
✅ **Offline Support** - Local SQLite storage for all data  

## Architecture

- **.NET MAUI** - Android mobile UI framework
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
│   └── CostSharingApp.Tests/      # Unit tests (88 tests)
└── specs/                          # Requirements and task documentation
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022 or Visual Studio Code
- Android SDK (API 35 or later)
- Android Emulator or physical device

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

All 88 unit tests should pass.

#### Android Emulator

```bash
dotnet build src/CostSharingApp/CostSharingApp.csproj -f net9.0-android -t:Run
```

Or open in Visual Studio and select Android Emulator target.

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

**Total: 88 tests, 100% passing**

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
