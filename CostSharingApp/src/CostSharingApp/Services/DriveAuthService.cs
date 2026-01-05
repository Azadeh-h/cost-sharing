using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace CostSharingApp.Services;

/// <summary>
/// Handles Google Drive OAuth authentication for native MAUI apps.
/// </summary>
public class DriveAuthService : IDriveAuthService
{
    private const string ApplicationName = "CostSharingApp";
    private static readonly string[] Scopes = { DriveService.Scope.DriveFile };

    private UserCredential? userCredential;

    /// <summary>
    /// Authenticates user with Google Drive using OAuth2 for native apps.
    /// </summary>
    /// <param name="clientId">Google OAuth client ID.</param>
    /// <param name="clientSecret">Google OAuth client secret.</param>
    /// <returns>True if authentication succeeded.</returns>
    public async Task<bool> AuthenticateAsync(string clientId, string clientSecret)
    {
        try
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            // Use native app flow with local redirect
            this.userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                "user",
                CancellationToken.None);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Drive auth failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets authenticated Drive service instance.
    /// </summary>
    /// <returns>Configured DriveService instance or null if not authenticated.</returns>
    public DriveService? GetDriveService()
    {
        if (this.userCredential == null)
        {
            return null;
        }

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = this.userCredential,
            ApplicationName = ApplicationName
        });
    }

    /// <summary>
    /// Checks if user is currently authenticated.
    /// </summary>
    /// <returns>True if authenticated.</returns>
    public bool IsAuthenticated()
    {
        return this.userCredential != null;
    }

    /// <summary>
    /// Clears authentication credentials.
    /// </summary>
    public void SignOut()
    {
        this.userCredential = null;
    }
}

/// <summary>
/// Interface for Google Drive authentication.
/// </summary>
public interface IDriveAuthService
{
    /// <summary>
    /// Authenticates with Google Drive.
    /// </summary>
    /// <param name="clientId">OAuth client ID.</param>
    /// <param name="clientSecret">OAuth client secret.</param>
    /// <returns>True if successful.</returns>
    Task<bool> AuthenticateAsync(string clientId, string clientSecret);

    /// <summary>
    /// Gets authenticated Drive service.
    /// </summary>
    /// <returns>DriveService instance or null.</returns>
    DriveService? GetDriveService();

    /// <summary>
    /// Checks authentication status.
    /// </summary>
    /// <returns>True if authenticated.</returns>
    bool IsAuthenticated();

    /// <summary>
    /// Signs out user.
    /// </summary>
    void SignOut();
}
