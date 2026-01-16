// <copyright file="IDriveSyncService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharing.Core.Services;

/// <summary>
/// Interface for Google Drive synchronization service.
/// </summary>
public interface IDriveSyncService
{
    /// <summary>
    /// Creates a Google Drive folder for a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The folder ID.</returns>
    Task<string> CreateGroupFolderAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets permissions on a folder for group members.
    /// </summary>
    /// <param name="folderId">The folder ID.</param>
    /// <param name="memberEmails">The member email addresses.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetFolderPermissionsAsync(string folderId, IEnumerable<string> memberEmails, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads group data to Google Drive.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UploadGroupDataAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads group data from Google Drive.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DownloadGroupDataAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if there are changes to sync.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if there are changes.</returns>
    Task<bool> CheckForChangesAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts periodic sync for a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartPeriodicSyncAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);
}
