// <copyright file="DriveAuthService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CostSharing.Core.Services;

namespace CostSharingApp.Services;

/// <summary>
/// Service for Google Drive OAuth authentication and token management.
/// </summary>
public class DriveAuthService : IDriveAuthService
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";

    private readonly ConfigurationService configService;
    private readonly ILoggingService loggingService;
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DriveAuthService"/> class.
    /// </summary>
    /// <param name="configService">Configuration service.</param>
    /// <param name="loggingService">Logging service.</param>
    public DriveAuthService(
        ConfigurationService configService,
        ILoggingService loggingService)
    {
        this.configService = configService;
        this.loggingService = loggingService;
        this.httpClient = new HttpClient();
    }

    /// <inheritdoc/>
    public async Task<bool> AuthorizeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            this.loggingService.LogInfo($"Starting OAuth authorization for user {userId}");
            
            var clientId = this.configService.GetValue("GoogleDrive:ClientId");
            var redirectUri = this.configService.GetValue("GoogleDrive:RedirectUri");
            var scopes = this.configService.GetValue("GoogleDrive:Scopes");

            this.loggingService.LogInfo($"OAuth config - ClientId: {(string.IsNullOrEmpty(clientId) ? "MISSING" : "present")}, RedirectUri: {redirectUri}, Scopes: {scopes}");

            if (string.IsNullOrEmpty(clientId))
            {
                this.loggingService.LogError("GoogleDrive:ClientId not configured in appsettings.json", null);
                throw new InvalidOperationException("GoogleDrive:ClientId not configured in appsettings.json");
            }

            // Build authorization URL
            var authUrl = $"{AuthorizationEndpoint}?" +
                         $"client_id={Uri.EscapeDataString(clientId)}&" +
                         $"redirect_uri={Uri.EscapeDataString(redirectUri ?? string.Empty)}&" +
                         $"response_type=code&" +
                         $"scope={Uri.EscapeDataString(scopes ?? string.Empty)}&" +
                         $"access_type=offline&" +
                         $"prompt=consent";

            this.loggingService.LogInfo($"Starting OAuth flow for user {userId}");

            // Launch web authentication
            WebAuthenticatorResult result;
            try
            {
                result = await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(authUrl),
                    new Uri(redirectUri ?? string.Empty));
            }
            catch (TaskCanceledException)
            {
                this.loggingService.LogInfo($"User cancelled authorization for user {userId}");
                return false;
            }
            catch (Exception ex)
            {
                this.loggingService.LogError($"WebAuthenticator failed: {ex.Message}", ex);
                throw new InvalidOperationException($"Failed to open browser for authorization: {ex.Message}", ex);
            }

            if (result.Properties.TryGetValue("code", out var authCode))
            {
                // Exchange authorization code for tokens
                var tokens = await this.ExchangeCodeForTokensAsync(authCode, cancellationToken);

                if (tokens != null)
                {
                    // Store tokens in secure storage
                    await SecureStorage.Default.SetAsync($"drive_access_token_{userId}", tokens.AccessToken);
                    await SecureStorage.Default.SetAsync($"drive_refresh_token_{userId}", tokens.RefreshToken ?? string.Empty);
                    await SecureStorage.Default.SetAsync($"drive_token_expiry_{userId}", tokens.ExpiresAt.ToString("o"));

                    this.loggingService.LogInfo($"Successfully authorized Drive access for user {userId}");
                    return true;
                }
            }

            this.loggingService.LogWarning($"Authorization failed for user {userId} - no auth code received");
            throw new InvalidOperationException("Authorization completed but no authorization code was received from Google.");
        }
        catch (TaskCanceledException)
        {
            this.loggingService.LogInfo($"User cancelled authorization for user {userId}");
            return false; // User cancelled is not an error
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw our own exceptions with their messages
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Authorization failed for user {userId}", ex);
            throw new InvalidOperationException($"Authorization failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAuthorizedAsync(Guid userId)
    {
        try
        {
            var accessToken = await SecureStorage.Default.GetAsync($"drive_access_token_{userId}");
            var refreshToken = await SecureStorage.Default.GetAsync($"drive_refresh_token_{userId}");

            return !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken);
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to check authorization status for user {userId}", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await SecureStorage.Default.GetAsync($"drive_access_token_{userId}");
            var expiryStr = await SecureStorage.Default.GetAsync($"drive_token_expiry_{userId}");

            if (string.IsNullOrEmpty(accessToken))
            {
                return null;
            }

            // Check if token is expired
            if (DateTime.TryParse(expiryStr, out var expiry))
            {
                if (expiry <= DateTime.UtcNow.AddMinutes(5))
                {
                    // Token expired or expiring soon, refresh it
                    this.loggingService.LogInfo($"Access token expired for user {userId}, refreshing...");
                    var refreshed = await this.RefreshAccessTokenAsync(userId, cancellationToken);
                    return refreshed ? await SecureStorage.Default.GetAsync($"drive_access_token_{userId}") : null;
                }
            }

            return accessToken;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to get access token for user {userId}", ex);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task RevokeAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await SecureStorage.GetAsync($"drive_access_token_{userId}");

            if (!string.IsNullOrEmpty(accessToken))
            {
                // Revoke token with Google
                var content = new StringContent(
                    $"token={accessToken}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

                var response = await this.httpClient.PostAsync(RevokeEndpoint, content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }

            // Clear stored tokens
            SecureStorage.Default.Remove($"drive_access_token_{userId}");
            SecureStorage.Default.Remove($"drive_refresh_token_{userId}");
            SecureStorage.Default.Remove($"drive_token_expiry_{userId}");

            this.loggingService.LogInfo($"Successfully revoked Drive authorization for user {userId}");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to revoke authorization for user {userId}", ex);
            throw;
        }
    }

    private async Task<OAuthTokenResponse?> ExchangeCodeForTokensAsync(string authCode, CancellationToken cancellationToken)
    {
        try
        {
            var clientId = this.configService.GetValue("GoogleDrive:ClientId");
            var clientSecret = this.configService.GetValue("GoogleDrive:ClientSecret");
            var redirectUri = this.configService.GetValue("GoogleDrive:RedirectUri");

            var requestData = new Dictionary<string, string>
            {
                { "code", authCode },
                { "client_id", clientId ?? string.Empty },
                { "client_secret", clientSecret ?? string.Empty },
                { "redirect_uri", redirectUri ?? string.Empty },
                { "grant_type", "authorization_code" },
            };

            var content = new FormUrlEncodedContent(requestData);
            var response = await this.httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

            return new OAuthTokenResponse
            {
                AccessToken = tokenData.GetProperty("access_token").GetString() ?? string.Empty,
                RefreshToken = tokenData.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32()),
            };
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to exchange authorization code for tokens", ex);
            return null;
        }
    }

    private async Task<bool> RefreshAccessTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = await SecureStorage.Default.GetAsync($"drive_refresh_token_{userId}");
            if (string.IsNullOrEmpty(refreshToken))
            {
                this.loggingService.LogWarning($"No refresh token found for user {userId}");
                return false;
            }

            var clientId = this.configService.GetValue("GoogleDrive:ClientId");
            var clientSecret = this.configService.GetValue("GoogleDrive:ClientSecret");

            var requestData = new Dictionary<string, string>
            {
                { "refresh_token", refreshToken },
                { "client_id", clientId ?? string.Empty },
                { "client_secret", clientSecret ?? string.Empty },
                { "grant_type", "refresh_token" },
            };

            var content = new FormUrlEncodedContent(requestData);
            var response = await this.httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

            var newAccessToken = tokenData.GetProperty("access_token").GetString();
            var expiresIn = tokenData.GetProperty("expires_in").GetInt32();

            if (!string.IsNullOrEmpty(newAccessToken))
            {
                await SecureStorage.Default.SetAsync($"drive_access_token_{userId}", newAccessToken);
                await SecureStorage.Default.SetAsync($"drive_token_expiry_{userId}", DateTime.UtcNow.AddSeconds(expiresIn).ToString("o"));

                this.loggingService.LogInfo($"Successfully refreshed access token for user {userId}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to refresh access token for user {userId}", ex);
            return false;
        }
    }

    private class OAuthTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string? RefreshToken { get; set; }

        public int ExpiresIn { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}
