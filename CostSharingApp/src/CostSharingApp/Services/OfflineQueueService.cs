using CostSharing.Core.Models;
using CostSharing.Core.Services;
using SQLite;
using System.Text.Json;

namespace CostSharingApp.Services;

/// <summary>
/// Service for managing offline queue of pending sync changes.
/// </summary>
public class OfflineQueueService : IOfflineQueueService
{
    private readonly SQLiteAsyncConnection database;
    private readonly IDriveSyncService driveSyncService;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineQueueService"/> class.
    /// </summary>
    public OfflineQueueService(SQLiteAsyncConnection database, IDriveSyncService driveSyncService)
    {
        this.database = database;
        this.driveSyncService = driveSyncService;
    }

    /// <inheritdoc/>
    public async Task QueueLocalChangeAsync(Guid groupId, string entityType, Guid entityId, string operationType, string payload)
    {
        var pendingSync = new PendingSync
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            EntityType = entityType,
            EntityId = entityId,
            OperationType = operationType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            LastError = null,
        };

        await this.database.InsertAsync(pendingSync);
        System.Diagnostics.Debug.WriteLine($"Queued change: {entityType} {operationType} for group {groupId}");
    }

    /// <inheritdoc/>
    public async Task<int> ProcessQueueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pendingItems = await this.database.Table<PendingSync>()
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        if (!pendingItems.Any())
        {
            return 0;
        }

        var successCount = 0;
        var failedItems = new List<PendingSync>();

        // Group by GroupId to process groups sequentially
        var groupedItems = pendingItems.GroupBy(p => p.GroupId);

        foreach (var group in groupedItems)
        {
            try
            {
                // Upload all changes for this group
                await this.driveSyncService.UploadGroupDataAsync(group.Key, userId, cancellationToken);

                // Mark all items for this group as successful
                foreach (var item in group)
                {
                    await this.database.DeleteAsync(item);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to process queue for group {group.Key}: {ex.Message}");

                // Update retry count for failed items
                foreach (var item in group)
                {
                    item.RetryCount++;
                    item.LastError = ex.Message;

                    // If too many retries, keep in queue but don't increment success
                    if (item.RetryCount < 5)
                    {
                        await this.database.UpdateAsync(item);
                    }
                    else
                    {
                        // Too many failures, log and remove
                        System.Diagnostics.Debug.WriteLine($"Giving up on sync item {item.Id} after {item.RetryCount} attempts");
                        await this.database.DeleteAsync(item);
                    }

                    failedItems.Add(item);
                }
            }
        }

        return successCount;
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingCountAsync(Guid groupId)
    {
        return await this.database.Table<PendingSync>()
            .Where(p => p.GroupId == groupId)
            .CountAsync();
    }

    /// <inheritdoc/>
    public async Task ClearSyncedItemsAsync(Guid groupId)
    {
        var items = await this.database.Table<PendingSync>()
            .Where(p => p.GroupId == groupId)
            .ToListAsync();

        foreach (var item in items)
        {
            await this.database.DeleteAsync(item);
        }
    }
}
