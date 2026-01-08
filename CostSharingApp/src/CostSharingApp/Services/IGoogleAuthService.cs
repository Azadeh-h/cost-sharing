
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;

namespace CostSharingApp.Services;
/// <summary>
/// Interface for Google authentication service.
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>
    /// Gets a value indicating whether the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's email address.
    /// </summary>
    string? CurrentUserEmail { get; }

    /// <summary>
    /// Authenticates the user with Google.
    /// </summary>
    /// <returns>True if authentication succeeded.</returns>
    Task<bool> AuthenticateAsync();

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task SignOutAsync();

    /// <summary>
    /// Initializes the service by loading existing credentials.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task InitializeAsync();

    /// <summary>
    /// Gets an authenticated Drive service instance.
    /// </summary>
    /// <returns>Drive service.</returns>
    DriveService GetDriveService();

    /// <summary>
    /// Gets an authenticated Gmail service instance.
    /// </summary>
    /// <returns>Gmail service.</returns>
    GmailService GetGmailService();
}
