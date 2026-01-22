// <copyright file="OAuthCallbackActivity.cs" company="CostSharingApp">
// Copyright (c) CostSharingApp. All rights reserved.
// </copyright>

using Android.App;
using Android.Content.PM;

namespace CostSharingApp.Platforms.Android;

/// <summary>
/// Activity for handling OAuth callback from Google using custom scheme redirect.
/// This activity is required by MAUI's WebAuthenticator to intercept the OAuth redirect.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { global::Android.Content.Intent.ActionView },
    Categories = new[] { global::Android.Content.Intent.CategoryDefault, global::Android.Content.Intent.CategoryBrowsable },
    DataScheme = "com.googleusercontent.apps.609247949564-c2h9s8bmg4100i7d7l2cfq0q4ofavcup",
    DataPath = "/oauth2redirect")]
public class OAuthCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
