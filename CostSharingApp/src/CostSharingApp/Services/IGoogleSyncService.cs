
using CostSharingApp.Models.GoogleSync;

namespace CostSharingApp.Services;
/// <summary>
/// Interface for Google synchronization service.
/// </summary>
public interface IGoogleSyncService
{
    /// <summary>
    /// Starts automatic synchronization with a specified interval.
    /// </summary>
    /// <param name="intervalSeconds">Sync interval in seconds.</param>
    void StartAutoSync(int intervalSeconds = 30);

    /// <summary>
    /// Stops automatic synchronization.
    /// </summary>
    void StopAutoSync();

    /// <summary>
    /// Syncs all groups with Google Drive.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task SyncAllGroupsAsync();

    /// <summary>
    /// Syncs a specific group with Google Drive.
    /// </summary>
    /// <param name="groupId">Group ID to sync.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SyncGroupAsync(Guid groupId);

    /// <summary>
    /// Resolves a conflict by choosing local or remote data.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="keepLocal">True to keep local changes, false to accept remote.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ResolveConflictAsync(Guid groupId, bool keepLocal);

    /// <summary>
    /// Gets the sync status for a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Sync metadata.</returns>
    Task<SyncMetadata> GetSyncStatusAsync(Guid groupId);

    /// <summary>
    /// Enables sync for a group by uploading it to Drive.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EnableSyncForGroupAsync(Guid groupId);

    /// <summary>
    /// Gets the Drive file ID for a synced group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Drive file ID or null if not synced.</returns>
    Task<string?> GetGroupDriveFileIdAsync(string groupId);

    /// <summary>
    /// Gets the last sync time.
    /// </summary>
    /// <returns>Last sync timestamp or null.</returns>
    DateTime? GetLastSyncTime();

    /// <summary>
    /// Syncs a specific group with Google Drive (string overload).
    /// </summary>
    /// <param name="groupId">Group ID as string.</param>
    /// <returns>Sync status.</returns>
    Task<CostSharingApp.Models.SyncStatus> SyncGroupAsync(string groupId);
}
