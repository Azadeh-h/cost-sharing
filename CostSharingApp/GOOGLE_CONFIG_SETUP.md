# Google OAuth Configuration - Secure Setup

## ✅ Client IDs Now in appsettings.json

Client IDs have been moved from source code to `appsettings.json` for better security practices.

## Setup Steps

### 1. Copy Template File
```bash
cp appsettings.template.json appsettings.json
```

### 2. Update appsettings.json

Edit `appsettings.json` and replace the placeholders:

```json
{
  "Google": {
    "AndroidClientId": "YOUR_ANDROID_CLIENT_ID.apps.googleusercontent.com",
    "iOSClientId": "YOUR_IOS_CLIENT_ID.apps.googleusercontent.com"
  },
  "GoogleDrive": {
    "ClientId": "YOUR_ANDROID_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "",
    "RedirectUri": "com.googleusercontent.apps.YOUR_CLIENT_ID:/oauth2redirect",
    "Scopes": "https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/gmail.send https://mail.google.com/"
  }
}
```

**Required OAuth Scopes:**
- `https://www.googleapis.com/auth/drive` - Full Google Drive access for syncing group data
- `https://www.googleapis.com/auth/gmail.send` - Send email on behalf of user (for invitations)
- `https://mail.google.com/` - Full Gmail access (required for reliable email sending)

**Your actual values:**
- Android: `609247949564-c2h9s8bmg4100i7d7l2cfq0q4ofavcup.apps.googleusercontent.com`
- iOS: `609247949564-2s9sgr82jhsqgpiaarhgua2vflbr1r3o.apps.googleusercontent.com`

### 3. Update AndroidManifest.xml

File: `Platforms/Android/AndroidManifest.xml`

Update line 16:
```xml
<data android:scheme="com.googleusercontent.apps.609247949564-c2h9s8bmg4100i7d7l2cfq0q4ofavcup" android:host="oauth2redirect" />
```

⚠️ **Note:** AndroidManifest.xml requires the scheme at compile time, so it must be hardcoded. This is safe because:
- The scheme is validated against your package signature (SHA-1)
- Without the matching signature, the OAuth flow will fail
- This is standard practice for Android OAuth

### 4. Files Protected by .gitignore

These files will NOT be committed:
- ✅ `appsettings.json` (contains your Client IDs)
- ✅ `appsettings.*.json` (any environment-specific configs)

These files WILL be committed:
- ✅ `appsettings.template.json` (template with placeholders)
- ✅ `AndroidManifest.xml` (Android requires scheme at compile time)

## Security Notes

### Why This Is Safe

1. **OAuth Client IDs are public** - designed to be embedded in mobile apps
2. **PKCE protection** - dynamic code verification prevents hijacking
3. **Signature validation** - Google validates your app's SHA-1 fingerprint
4. **Redirect URI validation** - Google validates the redirect scheme

### What to Keep Secret

- ❌ **Never commit** OAuth Client Secrets (server-side only, you don't have these)
- ❌ **Never commit** API keys (SendGrid, Twilio)
- ❌ **Never commit** user tokens or credentials

### What's Safe to Commit

- ✅ OAuth Client IDs (public identifiers with PKCE)
- ✅ AndroidManifest.xml with redirect scheme
- ✅ SHA-1 fingerprints (public, tied to your keystore)

## For New Team Members

1. Get the `appsettings.json` file from team lead (or create from template)
2. Ensure `appsettings.json` is in the same directory as `appsettings.template.json`
3. Build and run - OAuth will work automatically

## Production Deployment

For production builds:

1. Create `appsettings.Production.json` with production Client IDs
2. Use MSBuild conditions to include the right config
3. Update AndroidManifest.xml with production Client ID
4. Sign with production keystore (different SHA-1)
5. Add production SHA-1 to same OAuth clients in Google Console

## Troubleshooting

**Error: "Google:AndroidClientId not configured"**
- Solution: Copy `appsettings.template.json` to `appsettings.json` and update Client IDs

**OAuth fails on Android**
- Check: AndroidManifest.xml has correct scheme
- Check: SHA-1 matches the one in Google Console
- Check: Package name matches Google Console

**OAuth fails on iOS**
- Check: Bundle ID matches Google Console
- Check: Client ID is correct in appsettings.json

## Reference

Your SHA-1 Fingerprint: `E6:53:6D:74:9E:F9:C0:A4:C9:05:A1:43:8E:EB:83:22:1D:38:BE:BF`

## Gmail API Setup (Required for Email Invitations)

### Enable Gmail API in Google Cloud Console

1. Go to https://console.cloud.google.com
2. Select your project
3. Navigate to **APIs & Services → Library**
4. Search for **"Gmail API"**
5. Click on Gmail API and click **"Enable"**

### Add Gmail Scopes to OAuth Consent Screen

1. Go to **APIs & Services → OAuth consent screen**
2. Click **"Edit App"**
3. Click through to the **"Scopes"** section
4. Click **"Add or Remove Scopes"**
5. Search for and add these scopes:
   - `https://www.googleapis.com/auth/gmail.send`
   - `https://mail.google.com/`
6. Click **"Update"** and **"Save and Continue"**

### Re-authorize After Adding Scopes

After adding new scopes, users must re-authorize:
1. Open the app → **Settings**
2. Tap **"Revoke Authorization"**
3. Tap **"Authorize Google"**
4. Accept all permissions (should now include Gmail access)

**Note:** The Gmail API requires the `https://mail.google.com/` scope for reliable email sending. The `gmail.send` scope alone may not work for all use cases.
