# Phase 7: Testing - Implementation Notes

## Current Status

### ✅ Existing Tests (40 tests - All Passing)
The existing test suite in `tests/CostSharingApp.Tests/` covers:
- **Algorithms**: DebtSimplificationAlgorithm (8 tests)
- **Converters**: UI converters (8 tests)
- **Services**: Core business logic (14 tests)
- **ViewModels**: GroupDetailsViewModel, etc. (10 tests)

All tests are passing successfully.

## Testing Challenge: MAUI Project Structure

### The Problem
The Google integration code (services and ViewModels) is located in the `CostSharingApp` MAUI project, which is multi-targeted:
- `net9.0-android35.0`
- `net9.0-ios26.0`
- `net9.0-maccatalyst26.0`

The test project `CostSharingApp.Tests` targets standard `net9.0`, which **cannot reference a MAUI multi-targeted project**. This causes compilation errors:

```
error NU1201: Project CostSharingApp is not compatible with net9.0 (.NETCoreApp,Version=v9.0)
```

### Why This Matters
The following components cannot be unit tested with the current structure:
- ❌ `GoogleSyncService` (in CostSharingApp/Services/)
- ❌ `GoogleInvitationService` (in CostSharingApp/Services/)
- ❌ `GoogleAuthService` (in CostSharingApp/Services/)
- ❌ `GoogleSignInViewModel` (in CostSharingApp/ViewModels/)
- ❌ `SyncStatusViewModel` (in CostSharingApp/ViewModels/)
- ❌ `ConflictResolutionViewModel` (in CostSharingApp/ViewModels/)

## Solutions (Ranked by Effort vs. Benefit)

### Option 1: Move Services to Core Library ⭐ **RECOMMENDED**
**Effort**: Medium | **Benefit**: High | **Best Practice**: ✅

Move Google services to `CostSharing.Core`:
```
CostSharing.Core/
  Services/
    Google/
      GoogleSyncService.cs
      GoogleInvitationService.cs
      GoogleAuthService.cs
      IGoogleSyncService.cs
      IGoogleInvitationService.cs
      IGoogleAuthService.cs
```

**Pros**:
- Clean separation of business logic from UI
- Easy to unit test
- Follows MVVM/Clean Architecture principles
- Services become reusable across different UI frameworks

**Cons**:
- Requires moving files and updating namespaces
- Need to update all `using` statements in the MAUI app
- Google API package references need to be in Core project

**Implementation Steps**:
1. Move `Services/Google*.cs` files to `CostSharing.Core/Services/Google/`
2. Update namespaces from `CostSharingApp.Services` to `CostSharing.Core.Services.Google`
3. Move Google NuGet packages from CostSharingApp.csproj to CostSharing.Core.csproj
4. Update all `using` statements in MAUI app
5. Write unit tests in `CostSharingApp.Tests/Services/Google/`

---

### Option 2: Create Multi-Targeted Test Project
**Effort**: High | **Benefit**: Medium | **Best Practice**: ⚠️

Create a separate test project that targets MAUI platforms:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net9.0-android;net9.0-ios</TargetFrameworks>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\CostSharingApp\CostSharingApp.csproj" />
  </ItemGroup>
</Project>
```

**Pros**:
- No refactoring needed
- Can test code in its current location

**Cons**:
- Requires platform-specific test runners
- More complex CI/CD setup
- Slower test execution (platform-specific builds)
- Not standard practice for business logic testing

---

### Option 3: Manual/Integration Testing Only
**Effort**: Low | **Benefit**: Low | **Best Practice**: ❌

Skip unit tests for Google integration and rely on manual testing.

**Pros**:
- No code changes needed
- Quick to "complete"

**Cons**:
- No automated regression testing
- Harder to catch bugs early
- Not recommended for critical features like sync

---

## Recommended Path Forward

### Phase 7A: Refactor to Core (Week 1)
1. **Move Google services to `CostSharing.Core`**
   - Create `CostSharing.Core/Services/Google/` folder
   - Move GoogleSyncService, GoogleInvitationService, GoogleAuthService
   - Move interfaces (IGoogleSyncService, etc.)
   - Update namespaces to `CostSharing.Core.Services.Google`

2. **Update package references**
   - Add Google API packages to CostSharing.Core.csproj:
     ```xml
     <PackageReference Include="Google.Apis.Drive.v3" Version="1.73.0.3996" />
     <PackageReference Include="Google.Apis.Gmail.v1" Version="1.73.0.3987" />
     <PackageReference Include="Google.Apis.Auth" Version="1.73.0" />
     ```

3. **Update MAUI app references**
   - Update `using` statements throughout the app
   - Verify compilation: `dotnet build src/CostSharingApp/CostSharingApp.csproj`

### Phase 7B: Write Comprehensive Tests (Week 2)
1. **GoogleSyncService Tests** (13 tests)
   - StartAutoSync / StopAutoSync
   - SyncGroupAsync (success, conflict detection)
   - GetSyncStatusAsync
   - GetGroupDriveFileIdAsync
   - ResolveConflictAsync

2. **GoogleInvitationService Tests** (9 tests)
   - SendInvitationAsync (success, unauthenticated)
   - HandleInvitationDeepLinkAsync (various formats)
   - AcceptInvitationAsync

3. **GoogleAuthService Tests** (8 tests)
   - SignInAsync (success, failure)
   - SignOutAsync
   - RefreshTokenAsync
   - IsAuthenticatedAsync

4. **ViewModel Tests** (15 tests)
   - GoogleSignInViewModel
   - SyncStatusViewModel
   - ConflictResolutionViewModel

**Total**: ~45 new unit tests

---

## Integration Testing Strategy

Even with unit tests, you'll need integration testing for:

### Phase 8: Integration Tests
1. **OAuth Flow Testing**
   - Sign in on Android emulator
   - Sign in on iOS simulator
   - Token refresh scenarios

2. **Google Drive Integration**
   - Upload group data
   - Download group data
   - Conflict resolution (simulate offline edits)
   - Auto-sync behavior

3. **Gmail Integration**
   - Send invitation email
   - Deep link navigation
   - Accept invitation flow

### Testing Checklist
- [ ] Android emulator: Sign in → Create group → Sync → Sign out
- [ ] iOS simulator: Sign in → Join group via deep link
- [ ] Conflict scenario: Edit same group offline on 2 devices → Sync → Resolve
- [ ] Gmail: Send invitation → Receive email → Click link → Accept
- [ ] Auto-sync: Enable → Make changes → Wait 30s → Verify sync

---

## Current Test Coverage

```
Existing Tests: 40 (100% passing)
├── Algorithms: 8 tests ✅
├── Converters: 8 tests ✅
├── Services: 14 tests ✅
└── ViewModels: 10 tests ✅

Google Integration: 0 tests (awaiting refactor)
├── Services: 0 tests ⚠️
└── ViewModels: 0 tests ⚠️

Target Coverage: 85+ tests
├── Existing: 40 tests
└── New (after refactor): 45 tests
```

---

## Next Steps

### Immediate Actions
1. **Decide on approach**: Option 1 (refactor) vs Option 2 (multi-targeted tests) vs Option 3 (manual only)
2. **If choosing Option 1 (recommended)**:
   - Back up current code
   - Create feature branch: `feature/refactor-google-to-core`
   - Follow Phase 7A steps above
3. **If choosing Option 2**:
   - Research MAUI test project setup
   - Create `CostSharingApp.MAUI.Tests` project
4. **If choosing Option 3**:
   - Document manual test procedures
   - Create integration test checklist

### Before Production
Regardless of option chosen, **you must complete Phase 6** (Google Cloud Console setup) before any Google features will work:
- [ ] Create OAuth Client IDs
- [ ] Configure redirect URIs
- [ ] Enable Drive API
- [ ] Enable Gmail API
- [ ] Test on real devices

---

## Technical Notes

### Platform-Specific Code
The Google Auth platform implementations (Android/iOS) are already abstracted via:
- `Platforms/Android/GoogleAuthPlatform.cs` (uses WebAuthenticator)
- `Platforms/iOS/GoogleAuthPlatform.cs` (uses WebAuthenticator)

These can be mocked in tests using the `IGoogleAuthService` interface.

### Mocking Strategy
All Google services use dependency injection and interfaces:
```csharp
public GoogleSyncService(
    IGoogleAuthService authService,
    IGoogleDriveService driveService,
    ICacheService cacheService,
    IGroupService groupService,
    IExpenseService expenseService,
    ISettlementService settlementService)
```

This makes them easy to test with Moq:
```csharp
var mockAuth = new Mock<IGoogleAuthService>();
mockAuth.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(true);
```

### Test Data
Consider creating test fixtures:
- Sample Group JSON (for Drive sync)
- Sample Invitation emails (for Gmail)
- Mock OAuth tokens
- Conflict scenarios (local vs remote timestamps)

---

## Questions?

If you need help deciding on an approach or implementing any of these options, let me know! The recommended path is **Option 1** (refactor to Core), which will take ~1-2 weeks but provides the best long-term maintainability.
