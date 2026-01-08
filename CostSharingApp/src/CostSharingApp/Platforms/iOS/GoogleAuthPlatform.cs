
using Foundation;
using AuthenticationServices;
using System.Text;

namespace CostSharingApp.Platforms.iOS;
/// <summary>
/// Platform-specific Google OAuth implementation for iOS.
/// </summary>
public class GoogleAuthPlatform
{
    
    /// <summary>
    /// Performs OAuth authentication using ASWebAuthenticationSession.
    /// </summary>
    /// <param name="clientId">Google OAuth client ID.</param>
    /// <param name="scopes">OAuth scopes to request.</param>
    /// <returns>Authentication result with tokens.</returns>
    public static async Task<GoogleAuthResult> AuthenticateAsync(string clientId, string[] scopes)
    {
        try
        {
            var scopeString = string.Join(" ", scopes);
            var state = GenerateState();
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var redirectUri = $"com.googleusercontent.apps.{clientId.Split('-')[0]}:/oauth2redirect";
            
            // Build OAuth URL
            var authUrl = BuildAuthorizationUrl(clientId, redirectUri, scopeString, state, codeChallenge);
            
            // Use WebAuthenticator
            var authResult = await Microsoft.Maui.Authentication.WebAuthenticator.Default.AuthenticateAsync(
                new Uri(authUrl),
                new Uri(redirectUri));
            
            // Verify state
            if (authResult.Properties.TryGetValue("state", out var returnedState) && returnedState != state)
            {
                throw new InvalidOperationException("Invalid state parameter");
            }
            
            // Get authorization code
            if (!authResult.Properties.TryGetValue("code", out var authCode))
            {
                throw new InvalidOperationException("No authorization code received");
            }
            
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
            return new GoogleAuthResult { Success = false, Error = "Authentication cancelled" };
        }
        catch (Exception ex)
        {
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
