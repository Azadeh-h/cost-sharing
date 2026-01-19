using SQLite;

namespace CostSharing.Core.Models;

/// <summary>
/// Represents metadata about a sync operation.
/// </summary>
public class SyncMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier for this sync metadata record.
    /// </summary>
    [PrimaryKey]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the entity ID that was synced.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Gets or sets the group ID associated with this sync metadata.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the entity type (e.g., "Expense", "Group").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last sync timestamp.
    /// </summary>
    public DateTime LastSyncTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the remote file ID (e.g., Google Drive file ID).
    /// </summary>
    public string? RemoteFileId { get; set; }

    /// <summary>
    /// Gets or sets the remote file version or ETag.
    /// </summary>
    public string? RemoteVersion { get; set; }

    /// <summary>
    /// Gets or sets the local modification timestamp.
    /// </summary>
    public DateTime LocalModifiedTimestamp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there are local changes pending upload.
    /// </summary>
    public bool HasPendingChanges { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is a sync conflict.
    /// </summary>
    public bool HasConflict { get; set; }

    /// <summary>
    /// Gets or sets the details about the conflict if one exists.
    /// </summary>
    public string? ConflictDetails { get; set; }
}
