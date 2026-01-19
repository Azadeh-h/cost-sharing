using Android.App;
using Android.Content.PM;

namespace CostSharingApp.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { global::Android.Content.Intent.ActionView },
    Categories = new[] { global::Android.Content.Intent.CategoryDefault, global::Android.Content.Intent.CategoryBrowsable },
    DataScheme = "costsharingapp",
    DataHost = "join")]
[IntentFilter(
    new[] { global::Android.Content.Intent.ActionView },
    Categories = new[] { global::Android.Content.Intent.CategoryDefault, global::Android.Content.Intent.CategoryBrowsable },
    DataScheme = "com.costsharingapp.mobile",
    DataHost = "oauth2redirect")]
public class MainActivity : MauiAppCompatActivity
{
}
