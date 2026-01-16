// <copyright file="IDriveAuthService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharing.Core.Services;

/// <summary>
/// Interface for Google Drive OAuth authentication service.
/// </summary>
public interface IDriveAuthService
{
    /// <summary>
    /// Authorizes the user with Google Drive.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if authorization was successful.</returns>
    Task<bool> AuthorizeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user is authorized with Google Drive.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if authorized.</returns>
    Task<bool> IsAuthorizedAsync(Guid userId);

    /// <summary>
    /// Gets the access token for the user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token or null if not authorized.</returns>
    Task<string?> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the user's authorization.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default);
}
