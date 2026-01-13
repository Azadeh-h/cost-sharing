
using global::Android.App;
using global::Android.Content;
using global::AndroidX.Browser.CustomTabs;
using Microsoft.Maui.Authentication;
using System.Text;

namespace CostSharingApp.Platforms.Android;
/// <summary>
/// Platform-specific Google OAuth implementation for Android using Chrome Custom Tabs.
/// </summary>
public class GoogleAuthPlatform
{
    private static TaskCompletionSource<string>? authCompletionSource;
    
    /// <summary>
    /// Handles the OAuth callback with authorization code.
    /// </summary>
    public static void HandleCallback(string url)
    {
        if (authCompletionSource != null && !authCompletionSource.Task.IsCompleted)
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var code = query["code"];
            
            if (!string.IsNullOrEmpty(code))
            {
                global::Android.Util.Log.Debug("GoogleAuth", $"Authorization code received: {code.Substring(0, 10)}...");
                authCompletionSource.TrySetResult(code);
            }
            else
            {
                var error = query["error"] ?? "No authorization code received";
                global::Android.Util.Log.Error("GoogleAuth", $"OAuth error: {error}");
                authCompletionSource.TrySetException(new Exception(error));
            }
        }
    }
    
    /// <summary>
    /// Performs OAuth authentication using WebAuthenticator.
    /// </summary>
    /// <param name="clientId">Google OAuth client ID.</param>
    /// <param name="scopes">OAuth scopes to request.</param>
    /// <returns>Authentication result with tokens.</returns>
    public static async Task<GoogleAuthResult> AuthenticateAsync(string clientId, string[] scopes)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[GoogleAuth] Starting authentication with clientId: {clientId}");
            
            var scopeString = string.Join(" ", scopes);
            var state = GenerateState();
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            
            // Use ngrok HTTPS URL - we'll handle the callback manually
            var redirectUri = "https://annually-canelike-galilea.ngrok-free.dev/oauth2redirect";
            
            System.Diagnostics.Debug.WriteLine($"[GoogleAuth] Redirect URI: {redirectUri}");
            System.Diagnostics.Debug.WriteLine($"[GoogleAuth] Scopes: {scopeString}");
            
            // Build OAuth URL
            var authUrl = BuildAuthorizationUrl(clientId, redirectUri, scopeString, state, codeChallenge);
            
            global::Android.Util.Log.Debug("GoogleAuth", $"Auth URL: {authUrl}");
            
            // Create completion source for async callback handling
            authCompletionSource = new TaskCompletionSource<string>();
            
            // Launch browser with OAuth URL
            var context = global::Android.App.Application.Context;
            
            try
            {
                // Try Chrome Custom Tab first
                var customTabsIntent = new CustomTabsIntent.Builder()
                    .SetShowTitle(true)
                    .Build();
                customTabsIntent.Intent.SetFlags(ActivityFlags.NewTask);
                customTabsIntent.LaunchUrl(context, global::Android.Net.Uri.Parse(authUrl));
                
                global::Android.Util.Log.Debug("GoogleAuth", "Launched Chrome Custom Tab");
            }
            catch
            {
                // Fallback to regular browser intent
                var intent = new Intent(Intent.ActionView);
                intent.SetData(global::Android.Net.Uri.Parse(authUrl));
                intent.SetFlags(ActivityFlags.NewTask);
                context.StartActivity(intent);
                
                global::Android.Util.Log.Debug("GoogleAuth", "Launched fallback browser");
            }
            
            // Wait for callback with timeout
            var authCodeTask = authCompletionSource.Task;
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
            var completedTask = await Task.WhenAny(authCodeTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Authentication timed out");
            }
            
            var authCode = await authCodeTask;
            
            System.Diagnostics.Debug.WriteLine($"[GoogleAuth] Authorization code received");
            
            // Exchange code for tokens
            var tokens = await ExchangeCodeForTokensAsync(clientId, redirectUri, authCode, codeVerifier);
            
            // Get user info
            var userInfo = await GetUserInfoAsync(tokens.AccessToken);
            
            return new GoogleAuthResult
            {
                Success = true,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresIn = tokens.ExpiresIn,
                Email = userInfo.Email,
                Name = userInfo.Name,
            };
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[GoogleAuth] Authentication cancelled by user");
            return new GoogleAuthResult { Success = false, Error = "Authentication cancelled" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GoogleAuth] Authentication failed: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[GoogleAuth] Stack trace: {ex.StackTrace}");
            return new GoogleAuthResult { Success = false, Error = ex.Message };
        }
    }
    
    private static string BuildAuthorizationUrl(string clientId, string redirectUri, string scope, string state, string codeChallenge)
    {
        var url = "https://accounts.google.com/o/oauth2/v2/auth";
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = scope,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
        };
        
        var queryString = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return $"{url}?{queryString}";
    }
    
    private static async Task<TokenResponse> ExchangeCodeForTokensAsync(string clientId, string redirectUri, string authCode, string codeVerifier)
    {
        using var httpClient = new HttpClient();
        
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["code"] = authCode,
            ["code_verifier"] = codeVerifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
        };
        
        var content = new FormUrlEncodedContent(parameters);
        var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        
        return new TokenResponse
        {
            AccessToken = tokenData?["access_token"]?.ToString() ?? string.Empty,
            RefreshToken = tokenData?.ContainsKey("refresh_token") == true ? tokenData["refresh_token"]?.ToString() : null,
            ExpiresIn = tokenData?["expires_in"] != null ? Convert.ToInt32(tokenData["expires_in"]) : 3600,
        };
    }
    
    private static async Task<UserInfo> GetUserInfoAsync(string accessToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var userData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        
        return new UserInfo
        {
            Email = userData?["email"]?.ToString() ?? string.Empty,
            Name = userData?["name"]?.ToString() ?? string.Empty,
        };
    }
    
    private static string GenerateState()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
    
    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
    
    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}

/// <summary>
/// Result of Google authentication.
/// </summary>
public class GoogleAuthResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// OAuth token response.
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}

/// <summary>
/// User information from Google.
/// </summary>
public class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
