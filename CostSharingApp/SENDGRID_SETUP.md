# SendGrid Email Setup Guide

## Step 1: Create SendGrid Account
1. Go to https://app.sendgrid.com/signup
2. Sign up with your email (free tier: 100 emails/day)
3. Verify your email address

## Step 2: Verify Sender Identity
1. Log in to SendGrid dashboard
2. Go to **Settings > Sender Authentication**
3. Click **Verify a Single Sender**
4. Fill in your details:
   - From Name: `Cost Sharing App` (or your name)
   - From Email: Your verified email (e.g., `your.email@gmail.com`)
   - Reply To: Same as From Email
5. Check your email and click verification link

## Step 3: Create API Key
1. Go to **Settings > API Keys**
2. Click **Create API Key**
3. Name: `CostSharingApp`
4. Permissions: **Restricted Access**
   - Mail Send: **FULL ACCESS** (only this is needed)
5. Click **Create & View**
6. **COPY THE API KEY** (you can only see it once!)

## Step 4: Update App Configuration

Edit `src/CostSharingApp/appsettings.json`:

```json
{
  "SendGrid": {
    "ApiKey": "SG.xxxxxxxxxxxxxxxxxxxxx.yyyyyyyyyyyyyyyyyyyy",
    "FromEmail": "your.verified.email@gmail.com",
    "FromName": "Cost Sharing App"
  }
}
```

Replace:
- `ApiKey`: Your actual SendGrid API key (starts with `SG.`)
- `FromEmail`: The email you verified in Step 2

## Step 5: Rebuild and Test

```bash
cd /Users/azadehhassanzadeh/Source/cost-sharing/CostSharingApp
dotnet publish src/CostSharingApp/CostSharingApp.csproj -c Debug -f net9.0-android
```

## Testing Email Invitations

1. Open app, go to group
2. Click **Invite Member**
3. Enter email address
4. Click **Send Email Invitation**
5. Check logs for success/failure

## Troubleshooting

### Error: "Email send failed: 401 Unauthorized"
- API key is incorrect or expired
- Regenerate API key in SendGrid dashboard

### Error: "Email send failed: 403 Forbidden"
- From email not verified
- Go to Sender Authentication and verify your email

### Error: "Failed to send invitation email"
- Check internet connection
- Verify API key is correctly pasted (no extra spaces)
- Check SendGrid dashboard Activity Feed for details

## Free Tier Limits
- 100 emails per day forever
- Perfect for personal use or small groups
- Upgrade if you need more (paid plans start at $15/month for 40k emails)
