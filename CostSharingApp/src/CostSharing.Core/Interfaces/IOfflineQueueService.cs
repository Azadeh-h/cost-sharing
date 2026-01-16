// <copyright file="IOfflineQueueService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharing.Core.Services;

/// <summary>
/// Interface for managing offline sync queue.
/// </summary>
public interface IOfflineQueueService
{
    /// <summary>
    /// Queues a local change for sync.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="entityType">The entity type (Expense, Settlement, etc.).</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="operationType">The operation (Create, Update, Delete).</param>
    /// <param name="payload">JSON payload of the change.</param>
    Task QueueLocalChangeAsync(Guid groupId, string entityType, Guid entityId, string operationType, string payload);

    /// <summary>
    /// Processes all queued sync operations.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of items processed.</returns>
    Task<int> ProcessQueueAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of pending items for a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <returns>The pending count.</returns>
    Task<int> GetPendingCountAsync(Guid groupId);

    /// <summary>
    /// Clears synced items from the queue.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    Task ClearSyncedItemsAsync(Guid groupId);
}
