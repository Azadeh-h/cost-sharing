
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;

namespace CostSharingApp.Services;
/// <summary>
/// Service for handling Google authentication and API client creation.
/// </summary>
public class GoogleAuthService : IGoogleAuthService
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration configuration;
    private readonly string[] scopes = new[]
    {
        DriveService.Scope.DriveFile, // Access files created by app
        GmailService.Scope.GmailSend, // Send emails
        "email",
        "profile",
    };

    private UserCredential? userCredential;
    private string? currentUserEmail;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleAuthService"/> class.
    /// </summary>
    public GoogleAuthService(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Gets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated => this.userCredential != null;

    /// <summary>
    /// Gets the current user's email address.
    /// </summary>
    public string? CurrentUserEmail => this.currentUserEmail;

    /// <summary>
    /// Authenticates the user with Google.
    /// </summary>
    /// <returns>True if authentication succeeded.</returns>
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
#if ANDROID
            var clientId = this.configuration["Google:AndroidClientId"] ?? throw new InvalidOperationException("Google:AndroidClientId not configured");
            var result = await CostSharingApp.Platforms.Android.GoogleAuthPlatform.AuthenticateAsync(clientId, this.scopes);
#elif IOS || MACCATALYST
            var clientId = this.configuration["Google:iOSClientId"] ?? throw new InvalidOperationException("Google:iOSClientId not configured");
            var result = await CostSharingApp.Platforms.iOS.GoogleAuthPlatform.AuthenticateAsync(clientId, this.scopes);
#else
            throw new PlatformNotSupportedException("Google authentication is only supported on Android and iOS");
#endif

            if (result.Success && result.AccessToken != null)
            {
                // Store tokens securely
                await SecureStorage.Default.SetAsync("google_access_token", result.AccessToken);
                if (result.RefreshToken != null)
                {
                    await SecureStorage.Default.SetAsync("google_refresh_token", result.RefreshToken);
                }
                await SecureStorage.Default.SetAsync("google_token_expiry", DateTime.UtcNow.AddSeconds(result.ExpiresIn).ToString("O"));
                
                this.currentUserEmail = result.Email;
                if (result.Email != null)
                {
                    await SecureStorage.Default.SetAsync("google_user_email", result.Email);
                }
                
                // Create UserCredential from tokens
                await this.LoadCredentialFromStorageAsync();
                
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Authentication failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    public async Task SignOutAsync()
    {
        this.userCredential = null;
        this.currentUserEmail = null;
        
        // Clear secure storage
        SecureStorage.Default.Remove("google_access_token");
        SecureStorage.Default.Remove("google_refresh_token");
        SecureStorage.Default.Remove("google_token_expiry");
        SecureStorage.Default.Remove("google_user_email");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets an authenticated Drive service instance.
    /// </summary>
    /// <returns>Drive service.</returns>
    public DriveService GetDriveService()
    {
        if (this.userCredential == null)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = this.userCredential,
            ApplicationName = "Cost Sharing App",
        });
    }

    /// <summary>
    /// Gets an authenticated Gmail service instance.
    /// </summary>
    /// <returns>Gmail service.</returns>
    public GmailService GetGmailService()
    {
        if (this.userCredential == null)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = this.userCredential,
            ApplicationName = "Cost Sharing App",
        });
    }

    /// <summary>
    /// Initializes service by loading existing credentials if available.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await this.LoadCredentialFromStorageAsync();
            this.currentUserEmail = await SecureStorage.Default.GetAsync("google_user_email");
        }
        catch
        {
            // No existing credentials
        }
    }

    private async Task LoadCredentialFromStorageAsync()
    {
        var accessToken = await SecureStorage.Default.GetAsync("google_access_token");
        var refreshToken = await SecureStorage.Default.GetAsync("google_refresh_token");
        var expiryStr = await SecureStorage.Default.GetAsync("google_token_expiry");

        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        DateTime? expiry = null;
        if (DateTime.TryParse(expiryStr, out var parsedExpiry))
        {
            expiry = parsedExpiry;
        }

        // Create token response
        var token = new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresInSeconds = expiry.HasValue ? (long)(expiry.Value - DateTime.UtcNow).TotalSeconds : 3600,
            IssuedUtc = DateTime.UtcNow,
        };

        // Create flow for token management
        var clientId = this.configuration["Google:AndroidClientId"] ?? "YOUR_CLIENT_ID.apps.googleusercontent.com";
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
            },
            Scopes = this.scopes,
        });

        // Create credential
        this.userCredential = new UserCredential(flow, "user", token);
    }
}
