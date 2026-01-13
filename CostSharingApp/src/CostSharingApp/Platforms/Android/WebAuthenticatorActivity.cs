using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace CostSharingApp.Platforms.Android;

/// <summary>
/// Activity for handling OAuth callback from Google via HTTPS redirect.
/// Extracts the authorization code and passes it back to the authentication flow.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
              Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
              DataScheme = "https",
              DataHost = "annually-canelike-galilea.ngrok-free.dev",
              DataPath = "/oauth2redirect")]
public class WebAuthenticatorActivity : Activity
{
    protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        var uri = Intent?.Data?.ToString();
        if (!string.IsNullOrEmpty(uri))
        {
            global::Android.Util.Log.Debug("GoogleAuth", $"Callback received: {uri.Substring(0, 50)}...");
            GoogleAuthPlatform.HandleCallback(uri);
        }
        
        // Close this activity and return to the app
        Finish();
    }
}
