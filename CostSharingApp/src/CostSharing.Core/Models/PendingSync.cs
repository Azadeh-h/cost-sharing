// <copyright file="PendingSync.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharing.Core.Models
{
    using SQLite;

    /// <summary>
    /// Represents a pending synchronization operation in the offline queue.
    /// </summary>
    public class PendingSync
    {
        /// <summary>
        /// Gets or sets the unique identifier for this pending sync operation.
        /// </summary>
        [PrimaryKey]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the entity ID that needs to be synced.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Gets or sets the entity type (e.g., "Expense", "Group", "Settlement").
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of sync operation as a string (e.g., "Create", "Update", "Delete").
        /// </summary>
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the serialized entity data as JSON payload.
        /// </summary>
        public string? Payload { get; set; }

        /// <summary>
        /// Gets or sets when this sync operation was created/queued.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of retry attempts.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the last error message if sync failed.
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Gets or sets the group ID associated with this sync operation.
        /// </summary>
        public Guid GroupId { get; set; }
    }
}
