// <copyright file="ISessionService.cs" company="CostSharingApp">
// Copyright (c) CostSharingApp. All rights reserved.
// </copyright>

namespace CostSharingApp.Services;

/// <summary>
/// Interface for managing user session persistence.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Saves the current user session to secure storage.
    /// </summary>
    /// <param name="userId">The user ID to save.</param>
    /// <returns>Task for async operation.</returns>
    Task SaveSessionAsync(Guid userId);

    /// <summary>
    /// Retrieves the saved session user ID.
    /// </summary>
    /// <returns>The user ID if session exists, null otherwise.</returns>
    Task<Guid?> GetSessionAsync();

    /// <summary>
    /// Clears the saved session (sign out).
    /// </summary>
    /// <returns>Task for async operation.</returns>
    Task ClearSessionAsync();

    /// <summary>
    /// Checks if a valid session exists.
    /// </summary>
    /// <returns>True if session exists.</returns>
    Task<bool> HasSessionAsync();
}
