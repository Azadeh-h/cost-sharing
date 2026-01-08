
using SQLite;

namespace CostSharingApp.Models.GoogleSync;
/// <summary>
/// Metadata for tracking synchronization status of groups with Google Drive.
/// </summary>
[Table("SyncMetadata")]
public class SyncMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the group ID.
    /// </summary>
    [Indexed]
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the Google Drive file ID.
    /// </summary>
    public string? DriveFileId { get; set; }

    /// <summary>
    /// Gets or sets the last sync timestamp.
    /// </summary>
    public DateTime LastSyncTime { get; set; }

    /// <summary>
    /// Gets or sets the local last modified timestamp.
    /// </summary>
    public DateTime LocalLastModified { get; set; }

    /// <summary>
    /// Gets or sets the remote last modified timestamp.
    /// </summary>
    public DateTime RemoteLastModified { get; set; }

    /// <summary>
    /// Gets or sets the sync status.
    /// </summary>
    public SyncStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets any error message from last sync attempt.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
