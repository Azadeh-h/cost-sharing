# Google Drive & Gmail Integration - Phase 4 Complete

## What's Been Implemented

### Phase 1 Complete ✅
#### 1. NuGet Packages Added ✅
- `Google.Apis.Drive.v3` (v1.73.0.3996) - For storing group data
- `Google.Apis.Gmail.v1` (v1.73.0.3987) - For sending invitation emails
- `Google.Apis.Auth` (v1.73.0) - For Google authentication

#### 2. Models Created ✅
- **GroupSyncDto** - Data structure for syncing group data with Drive
  - Contains Group, Members, Expenses, ExpenseSplits, Settlements
  - Includes version control and last modified tracking
  
- **SyncStatus** - Enum for tracking sync state
  - Synced, PendingUpload, PendingDownload, Syncing, Error, Conflict, NotSynced
  
- **SyncMetadata** - SQLite entity for tracking sync state per group
  - Tracks DriveFileId, timestamps, versions, errors

#### 3. Services Created ✅
- **GoogleAuthService** - Handles Google OAuth authentication
  - Manages user credentials
  - Provides DriveService and GmailService instances
  - Note: OAuth flow needs platform-specific implementation
  
- **GoogleDriveService** - Manages group data in Drive
  - Upload/download group data as JSON files
  - Creates folder structure: `CostSharingApp/groups/{groupId}.json`
  - Share files with other users
  - List accessible group files

### Phase 2 Complete ✅
#### 4. Sync Orchestration Service ✅
- **GoogleSyncService** - Orchestrates bidirectional sync between local SQLite and Google Drive
  - **Auto-sync**: Periodic polling every 30 seconds (configurable)
  - **Manual sync**: Sync individual groups or all groups
  - **Conflict detection**: Version-based comparison with timestamps
  - **Conflict resolution**: User choice to keep local or remote changes
  - **New group detection**: Downloads groups shared with user
  - **Status tracking**: Synced, Syncing, Error, Conflict states
  
#### 5. Database Updates ✅
- **CacheService** - Updated to manage SyncMetadata table
  - CreateTableAsync includes SyncMetadata
  - DropTableAsync includes SyncMetadata
  - Full CRUD support for sync tracking

#### 6. Dependency Injection ✅
- **MauiProgram.cs** - Registered all Google services
  - IGoogleAuthService → GoogleAuthService
  - IGoogleDriveService → GoogleDriveService
  - IGoogleSyncService → GoogleSyncService
  - IGoogleInvitationService → GoogleInvitationService

### Phase 3 Complete ✅
#### 7. Gmail Invitation Service ✅
- **GoogleInvitationService** - Send invitations via Gmail API
  - **Send invitations**: HTML email with deep link button
  - **Auto-share Drive file**: Invitee gets access to group data
  - **Deep link handling**: Parse costsharingapp://join?groupId=xxx
  - **Accept invitation**: Validates and triggers sync
  - **Beautiful HTML emails**: Styled invitation template
  
#### 8. Email Features ✅
- **Professional HTML email** with styled button
- **Deep link** for one-tap group joining
- **From address**: Uses authenticated user's Gmail
- **Subject line**: "You're invited to join [Group Name]"
- **Fallback link**: Copy-paste option if button doesn't work

### Phase 4 Complete ✅
#### 9. Platform-Specific OAuth ✅
- **Android Implementation** - WebAuthenticator with PKCE flow
  - Uses WebAuthenticator for OAuth 2.0
  - PKCE (Proof Key for Code Exchange) for security
  - Intent filter for OAuth callback
  - Deep link support for invitations
  
- **iOS Implementation** - WebAuthenticator support
  - Uses WebAuthenticator (wraps ASWebAuthenticationSession)
  - PKCE flow matching Android
  - URL scheme handling
  - Deep link support for invitations
  
#### 10. Token Management ✅
- **Secure Storage**: Tokens stored in platform SecureStorage
- **Auto-refresh**: Credential handles token refresh automatically
- **Persistence**: Credentials survive app restarts
- **Sign out**: Complete token cleanup

#### 11. Android Configuration ✅
- **AndroidManifest.xml**: OAuth callback and deep link intent filters
- **WebAuthenticatorCallbackActivity**: Handles OAuth redirect
- **Deep link handler**: Processes invitation links

## Sync Flow Details

### How Sync Works

1. **Initial Upload (New Group)**
   ```
   User creates group → GoogleSyncService.EnableSyncForGroupAsync()
   → Collects all group data (Group, Members, Expenses, etc.)
   → Uploads to Drive as JSON: CostSharingApp/groups/{groupId}.json
   → Saves SyncMetadata with DriveFileId and version
   ```

2. **Periodic Sync (Every 30s)**
   ```
   Timer triggers → SyncAllGroupsAsync()
   → For each group:
      - Get local data timestamp
      - Get remote data timestamp from Drive
      - Compare versions and timestamps
      - If remote newer: Download and apply changes
      - If local newer: Upload changes
      - If both changed: Mark as Conflict
   ```

3. **Conflict Resolution**
   ```
   User sees conflict indicator → Opens conflict dialog
   → Chooses "Keep Local" or "Accept Remote"
   → ResolveConflictAsync(groupId, keepLocal)
      - If keepLocal: Upload local with version++
      - If remote: Download and overwrite local
   → Status set to Synced
   ```

4. **Shared Group Detection**
   ```
   Sync runs → DownloadRemoteGroupsAsync()
   → Lists all files in Drive the user has access to
   → For each file:
      - If groupId not in local database:
         → Download full group data
         → Save to local SQLite
         → Create SyncMetadata
   ```

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│ CostSharing App (Local SQLite)                      │
│                                                      │
│  ┌──────────────┐        ┌──────────────┐         │
│  │ GroupService │◄──────►│  SyncService │         │
│  └──────────────┘        └──────┬───────┘         │
│                                  │                  │
│  ┌──────────────┐        ┌──────▼───────┐         │
│  │ CacheService │◄──────►│SyncMetadata  │         │
│  │(SQLite DB)   │        │  Table       │         │
│  └──────────────┘        └──────────────┘         │
└──────────────────────────────────┼──────────────────┘
                                   │
                    ┌──────────────▼──────────────┐
                    │  GoogleDriveService         │
                    │  - Upload/Download JSON     │
                    │  - Share with users         │
                    │  - List accessible files    │
                    └──────────────┬──────────────┘
                                   │
                    ┌──────────────▼──────────────┐
                    │  Google Drive               │
                    │                              │
                    │  CostSharingApp/            │
                    │    └── groups/              │
                    │         ├── {guid1}.json    │
                    │         └── {guid2}.json    │
                    └─────────────────────────────┘
```

## What's Next

### Phase 5: UI Updates (TODO)
- [ ] Create `GoogleSyncService` to orchestrate sync operations
- [ ] Implement sync logic: Local SQLite ↔ Drive JSON
- [ ] Handle conflict resolution (timestamp-based)
- [ ] Add periodic sync polling (every 30 seconds)
- [ ] Update CacheService to create SyncMetadata table

### Phase 3: Gmail Invitation Service (TODO)
- [ ] Create `GoogleInvitationService`
- [ ] Implement email template for invitations
- [ ] Send invitation with deep link: `costsharingapp://join?groupId=xxx`
- [ ] Handle deep link opening in app
- [ ] Auto-share Drive file when sending invitation

### Phase 5: UI Updates (TODO)
- [ ] Create Google sign-in screen/button
- [ ] Add sync status indicator in app bar
- [ ] Display "Last synced: X minutes ago"
- [ ] Add manual sync button in group details
- [ ] Show conflict resolution dialog when needed
- [ ] Update invitation flow UI to show Gmail option
- [ ] Add loading indicators during sync operations

### Phase 5: UI Updates (TODO)
- [ ] Add Google Sign-In button to login screen
- [ ] Show sync status indicator in app bar
- [ ] Display "Last synced: X minutes ago"
- [ ] Add manual sync button
- [ ] Show conflict resolution dialog when needed
- [ ] Update invitation flow to use Gmail

### Phase 6: Google Cloud Console Setup (TODO - USER ACTION REQUIRED)
**This requires manual setup in Google Cloud Console:**
1. Go to https://console.cloud.google.com
2. Create a new project (or select existing)
3. Enable Google Drive API
4. Enable Gmail API
5. Create OAuth 2.0 Client ID:
   - Application type: "Android" (for Android app)
   - Application type: "iOS" (for iOS app)
   - Package name: `com.costsharingapp.mobile`
   - Get SHA-1 certificate fingerprint: `keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android`
6. Download OAuth client configuration
7. Copy Client ID to replace `YOUR_CLIENT_ID` in:
   - `Platforms/Android/GoogleAuthPlatform.cs`
   - `Platforms/iOS/GoogleAuthPlatform.cs`
   - `Services/GoogleAuthService.cs`
   - `Platforms/Android/AndroidManifest.xml`
8. Configure OAuth consent screen:
   - Add test users for development
   - Set scopes: Drive (drive.file), Gmail (gmail.send), email, profile
9. For deep links, register custom URL scheme in console

### Phase 7: Testing (TODO)
- [ ] Unit tests for GoogleDriveService
- [ ] Unit tests for GoogleSyncService
- [ ] Integration tests with mock Drive API
- [ ] Test conflict resolution scenarios
- [ ] Test invitation flow end-to-end

## Files Created

### Phase 1 Files
```
src/CostSharingApp/
├── Models/
│   └── GoogleSync/
│       ├── GroupSyncDto.cs
│       ├── SyncStatus.cs
│       └── SyncMetadata.cs
└── Services/
    ├── GoogleAuthService.cs
    ├── IGoogleAuthService.cs
    ├── GoogleDriveService.cs
    └── IGoogleDriveService.cs
```

### Phase 2 Files
```
src/CostSharingApp/
└── Services/
    ├── GoogleSyncService.cs      (NEW - 450+ lines)
    ├── IGoogleSyncService.cs     (NEW)
    ├── CacheService.cs           (UPDATED - Added SyncMetadata table)
    └── MauiProgram.cs            (UPDATED - Registered Google services)
```

### Phase 3 Files
```
src/CostSharingApp/
└── Services/
    ├── GoogleInvitationService.cs  (NEW - 260+ lines)
    ├── IGoogleInvitationService.cs (NEW)
    └── MauiProgram.cs              (UPDATED - Registered invitation service)
```

### Phase 4 Files
```
src/CostSharingApp/
├── Platforms/
│   ├── Android/
│   │   ├── GoogleAuthPlatform.cs    (NEW - 210+ lines)
│   │   └── AndroidManifest.xml      (UPDATED - OAuth + deep link intents)
│   └── iOS/
│       └── GoogleAuthPlatform.cs    (NEW - 210+ lines)
└── Services/
    ├── GoogleAuthService.cs         (UPDATED - Platform OAuth integration)
    └── IGoogleAuthService.cs        (UPDATED - Added InitializeAsync)
```

## Current Branch
`feature/google-drive-sync`

## How It Will Work (User Experience)

1. **First Time Setup**
   - User opens app → "Sign in with Google" screen
   - User authenticates → App gets Drive & Gmail access
   - User email becomes their identity

2. **Creating a Group**
   - User creates group locally
   - SyncService automatically uploads to Drive
   - Creates `{groupId}.json` in Drive
   - Group is now "synced"

3. **Inviting Members**
   - User enters friend's email
   - App shares Drive file with friend's email
   - Gmail API sends invitation email with deep link
   - Friend receives email, clicks link
   - Friend's app opens, downloads group from Drive

4. **Making Changes**
   - Any user edits expense/settlement
   - Change saved locally immediately
   - SyncService uploads to Drive (within 5 seconds)
   - Other users' apps poll Drive every 30 seconds
   - Changes appear on all devices

5. **Conflict Resolution**
   - If two users edit simultaneously
   - App detects conflict (version mismatch)
   - Shows merge dialog: "Keep yours" or "Keep theirs"
   - User resolves, new version uploaded

## Cost Analysis

All Google APIs used are **FREE**:
- **Google Drive API**: Free (15GB storage per user)
- **Gmail API**: Free (unlimited sends for personal use)
- **OAuth 2.0**: Free
- **Rate Limits**: 
  - Drive: 1,000 requests per 100 seconds (more than enough)
  - Gmail: 250 sends per day (plenty for invitations)

## Next Steps for Development

**Phase 4 is now complete!** ✅

**Completed So Far:**
- ✅ Phase 1: Foundation (Models + Core Services)
- ✅ Phase 2: Sync Orchestration (GoogleSyncService)
- ✅ Phase 3: Invitation System (GoogleInvitationService)
- ✅ Phase 4: Platform OAuth (Android + iOS authentication)

**Ready to proceed with:**
- **Phase 5**: UI Updates (sign-in screen, sync indicators, manual sync)
- **Phase 6**: Google Cloud Console Setup (**USER ACTION - Required before testing**)
- **Phase 7**: Testing (unit tests, integration tests)

**CRITICAL NEXT STEP - Phase 6 (User Action):**
Before the OAuth implementation can work, you MUST set up Google Cloud Console:
1. Create OAuth 2.0 Client IDs for Android and iOS
2. Get your Client ID and replace all `YOUR_CLIENT_ID` placeholders in the code
3. Get SHA-1 fingerprint for Android debug keystore
4. Configure OAuth consent screen with test users

Without completing Phase 6, authentication will fail with "invalid client" errors.

Run these commands to test the build:
```bash
# Make sure we're on the right branch
git status

# Build to check for errors
cd /Users/azadehhassanzadeh/Source/cost-sharing/CostSharingApp
dotnet build src/CostSharingApp/CostSharingApp.csproj
```

## Dependencies Added

```xml
<PackageReference Include="Google.Apis.Drive.v3" Version="1.73.0.3996" />
<PackageReference Include="Google.Apis.Gmail.v1" Version="1.73.0.3987" />
<PackageReference Include="Google.Apis.Auth" Version="1.73.0" />
```
