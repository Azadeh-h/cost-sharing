using Android.App;
using Android.Content;
using Android.Content.PM;

namespace CostSharingApp.Platforms.Android;

/// <summary>
/// Activity for handling deep link callback with OAuth code from browser.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
              Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
              DataScheme = "costsharingapp",
              DataHost = "oauth")]
public class DeepLinkActivity : Activity
{
    protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        var data = Intent?.Data;
        if (data != null)
        {
            var code = data.GetQueryParameter("code");
            var fullUrl = $"https://annually-canelike-galilea.ngrok-free.dev/oauth2redirect?code={code}&state={data.GetQueryParameter("state")}";
            
            global::Android.Util.Log.Debug("GoogleAuth", $"Deep link received with code");
            GoogleAuthPlatform.HandleCallback(fullUrl);
        }
        
        // Close this activity
        Finish();
    }
}
