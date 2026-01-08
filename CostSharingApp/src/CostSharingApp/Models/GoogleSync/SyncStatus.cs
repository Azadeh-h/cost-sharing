namespace CostSharingApp.Models.GoogleSync;

/// <summary>
/// Represents the synchronization status of a group.
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Group is fully synced with Drive.
    /// </summary>
    Synced,

    /// <summary>
    /// Group has pending changes to upload.
    /// </summary>
    PendingUpload,

    /// <summary>
    /// Group has remote changes to download.
    /// </summary>
    PendingDownload,

    /// <summary>
    /// Synchronization is in progress.
    /// </summary>
    Syncing,

    /// <summary>
    /// Synchronization failed.
    /// </summary>
    Error,

    /// <summary>
    /// Conflict detected between local and remote data.
    /// </summary>
    Conflict,

    /// <summary>
    /// Group is not synced (local only).
    /// </summary>
    NotSynced,
}
