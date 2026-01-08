
using System.Diagnostics;
using CostSharing.Core.Models;
using CostSharingApp.Models.GoogleSync;

namespace CostSharingApp.Services;
/// <summary>
/// Service for synchronizing local data with Google Drive.
/// </summary>
public class GoogleSyncService : IGoogleSyncService
{
    private readonly IGoogleAuthService googleAuthService;
    private readonly IGoogleDriveService googleDriveService;
    private readonly ICacheService cacheService;
    private readonly IGroupService groupService;
    private readonly IExpenseService expenseService;
    private readonly ISettlementService settlementService;
    private readonly ILoggingService loggingService;
    private Timer? syncTimer;
    private bool isSyncing;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleSyncService"/> class.
    /// </summary>
    public GoogleSyncService(
        IGoogleAuthService googleAuthService,
        IGoogleDriveService googleDriveService,
        ICacheService cacheService,
        IGroupService groupService,
        IExpenseService expenseService,
        ISettlementService settlementService,
        ILoggingService loggingService)
    {
        this.googleAuthService = googleAuthService;
        this.googleDriveService = googleDriveService;
        this.cacheService = cacheService;
        this.groupService = groupService;
        this.expenseService = expenseService;
        this.settlementService = settlementService;
        this.loggingService = loggingService;
    }

    /// <summary>
    /// Starts automatic synchronization with a specified interval.
    /// </summary>
    /// <param name="intervalSeconds">Sync interval in seconds.</param>
    public void StartAutoSync(int intervalSeconds = 30)
    {
        this.syncTimer?.Dispose();
        this.syncTimer = new Timer(
            async _ => await this.SyncAllGroupsAsync(),
            null,
            TimeSpan.FromSeconds(intervalSeconds),
            TimeSpan.FromSeconds(intervalSeconds));
        
        this.loggingService.LogInfo($"Auto-sync started with {intervalSeconds}s interval");
    }

    /// <summary>
    /// Stops automatic synchronization.
    /// </summary>
    public void StopAutoSync()
    {
        this.syncTimer?.Dispose();
        this.syncTimer = null;
        this.loggingService.LogInfo("Auto-sync stopped");
    }

    /// <summary>
    /// Syncs all groups with Google Drive.
    /// </summary>
    public async Task SyncAllGroupsAsync()
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            this.loggingService.LogWarning("Cannot sync: User not authenticated");
            return;
        }

        if (this.isSyncing)
        {
            this.loggingService.LogInfo("Sync already in progress, skipping");
            return;
        }

        this.isSyncing = true;
        try
        {
            // Get all local groups
            var groups = await this.groupService.GetUserGroupsAsync();
            
            foreach (var group in groups)
            {
                try
                {
                    await this.SyncGroupAsync(group.Id);
                }
                catch (Exception ex)
                {
                    this.loggingService.LogError($"Failed to sync group {group.Id}: {ex.Message}");
                }
            }

            // Download any remote groups not present locally
            await this.DownloadRemoteGroupsAsync();
        }
        finally
        {
            this.isSyncing = false;
        }
    }

    /// <summary>
    /// Syncs a specific group with Google Drive.
    /// </summary>
    /// <param name="groupId">Group ID to sync.</param>
    public async Task SyncGroupAsync(Guid groupId)
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            throw new InvalidOperationException("User not authenticated");
        }

        var metadata = await this.GetOrCreateSyncMetadataAsync(groupId);
        
        try
        {
            metadata.Status = SyncStatus.Syncing;
            await this.SaveSyncMetadataAsync(metadata);

            // Get local data
            var localData = await this.GetLocalGroupDataAsync(groupId);
            
            if (metadata.DriveFileId == null)
            {
                // First time upload
                await this.UploadNewGroupAsync(groupId, localData, metadata);
            }
            else
            {
                // Check for remote changes
                var remoteData = await this.googleDriveService.DownloadGroupDataAsync(metadata.DriveFileId);
                
                if (remoteData == null)
                {
                    // Remote file deleted, re-upload
                    await this.UploadNewGroupAsync(groupId, localData, metadata);
                }
                else if (remoteData.LastModified > metadata.RemoteLastModified)
                {
                    // Remote has newer changes
                    if (localData.LastModified > metadata.LocalLastModified)
                    {
                        // Conflict: both local and remote changed
                        metadata.Status = SyncStatus.Conflict;
                        metadata.ErrorMessage = "Conflict detected: both local and remote have changes";
                        await this.SaveSyncMetadataAsync(metadata);
                        this.loggingService.LogWarning($"Conflict detected for group {groupId}");
                    }
                    else
                    {
                        // Download remote changes
                        await this.ApplyRemoteChangesAsync(groupId, remoteData, metadata);
                    }
                }
                else if (localData.LastModified > metadata.LocalLastModified)
                {
                    // Local has newer changes, upload
                    await this.UploadGroupChangesAsync(groupId, localData, metadata);
                }
                else
                {
                    // No changes on either side
                    metadata.Status = SyncStatus.Synced;
                    metadata.LastSyncTime = DateTime.UtcNow;
                    await this.SaveSyncMetadataAsync(metadata);
                }
            }
        }
        catch (Exception ex)
        {
            metadata.Status = SyncStatus.Error;
            metadata.ErrorMessage = ex.Message;
            await this.SaveSyncMetadataAsync(metadata);
            throw;
        }
    }

    /// <summary>
    /// Resolves a conflict by choosing local or remote data.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="keepLocal">True to keep local changes, false to accept remote.</param>
    public async Task ResolveConflictAsync(Guid groupId, bool keepLocal)
    {
        var metadata = await this.GetOrCreateSyncMetadataAsync(groupId);
        
        if (metadata.Status != SyncStatus.Conflict)
        {
            throw new InvalidOperationException("No conflict to resolve");
        }

        if (keepLocal)
        {
            // Upload local changes, overwriting remote
            var localData = await this.GetLocalGroupDataAsync(groupId);
            localData.Version = metadata.Version + 1;
            await this.UploadGroupChangesAsync(groupId, localData, metadata);
        }
        else
        {
            // Download remote changes, overwriting local
            if (metadata.DriveFileId != null)
            {
                var remoteData = await this.googleDriveService.DownloadGroupDataAsync(metadata.DriveFileId);
                if (remoteData != null)
                {
                    await this.ApplyRemoteChangesAsync(groupId, remoteData, metadata);
                }
            }
        }
    }

    /// <summary>
    /// Gets the sync status for a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Sync metadata.</returns>
    public async Task<SyncMetadata> GetSyncStatusAsync(Guid groupId)
    {
        return await this.GetOrCreateSyncMetadataAsync(groupId);
    }

    /// <summary>
    /// Enables sync for a group by uploading it to Drive.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    public async Task EnableSyncForGroupAsync(Guid groupId)
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            throw new InvalidOperationException("User not authenticated");
        }

        var metadata = await this.GetOrCreateSyncMetadataAsync(groupId);
        
        if (metadata.DriveFileId != null)
        {
            this.loggingService.LogInfo($"Group {groupId} already synced");
            return;
        }

        var localData = await this.GetLocalGroupDataAsync(groupId);
        await this.UploadNewGroupAsync(groupId, localData, metadata);
    }

    private async Task<GroupSyncDto> GetLocalGroupDataAsync(Guid groupId)
    {
        var group = await this.groupService.GetGroupAsync(groupId);
        if (group == null)
        {
            throw new Exception($"Group {groupId} not found");
        }

        var members = await this.groupService.GetGroupMembersAsync(groupId);
        var expenses = await this.expenseService.GetGroupExpensesAsync(groupId);
        var settlements = await this.settlementService.GetGroupSettlementsAsync(groupId);
        
        var expenseSplits = new List<ExpenseSplit>();
        foreach (var expense in expenses)
        {
            var splits = await this.expenseService.GetExpenseSplitsAsync(expense.Id);
            expenseSplits.AddRange(splits);
        }

        return new GroupSyncDto
        {
            Group = group,
            Members = members,
            Expenses = expenses,
            ExpenseSplits = expenseSplits,
            Settlements = settlements,
            LastModified = DateTime.UtcNow,
            LastModifiedBy = this.googleAuthService.CurrentUserEmail,
        };
    }

    private async Task UploadNewGroupAsync(Guid groupId, GroupSyncDto localData, SyncMetadata metadata)
    {
        this.loggingService.LogInfo($"Uploading new group {groupId}");
        
        localData.Version = 1;
        var fileId = await this.googleDriveService.UploadGroupDataAsync(localData);
        
        metadata.DriveFileId = fileId;
        metadata.Status = SyncStatus.Synced;
        metadata.Version = 1;
        metadata.LocalLastModified = localData.LastModified;
        metadata.RemoteLastModified = localData.LastModified;
        metadata.LastSyncTime = DateTime.UtcNow;
        metadata.ErrorMessage = null;
        
        await this.SaveSyncMetadataAsync(metadata);
        this.loggingService.LogInfo($"Group {groupId} uploaded successfully");
    }

    private async Task UploadGroupChangesAsync(Guid groupId, GroupSyncDto localData, SyncMetadata metadata)
    {
        this.loggingService.LogInfo($"Uploading changes for group {groupId}");
        
        localData.Version = metadata.Version + 1;
        await this.googleDriveService.UploadGroupDataAsync(localData, metadata.DriveFileId);
        
        metadata.Status = SyncStatus.Synced;
        metadata.Version = localData.Version;
        metadata.LocalLastModified = localData.LastModified;
        metadata.RemoteLastModified = localData.LastModified;
        metadata.LastSyncTime = DateTime.UtcNow;
        metadata.ErrorMessage = null;
        
        await this.SaveSyncMetadataAsync(metadata);
        this.loggingService.LogInfo($"Group {groupId} changes uploaded");
    }

    private async Task ApplyRemoteChangesAsync(Guid groupId, GroupSyncDto remoteData, SyncMetadata metadata)
    {
        this.loggingService.LogInfo($"Applying remote changes for group {groupId}");
        
        // Update group
        if (remoteData.Group != null)
        {
            await this.cacheService.SaveAsync(remoteData.Group);
        }

        // Clear and re-add members
        var existingMembers = await this.groupService.GetGroupMembersAsync(groupId);
        foreach (var member in existingMembers)
        {
            await this.cacheService.DeleteAsync(member);
        }
        foreach (var member in remoteData.Members)
        {
            await this.cacheService.SaveAsync(member);
        }

        // Clear and re-add expenses and splits
        var existingExpenses = await this.expenseService.GetGroupExpensesAsync(groupId);
        foreach (var expense in existingExpenses)
        {
            var splits = await this.expenseService.GetExpenseSplitsAsync(expense.Id);
            foreach (var split in splits)
            {
                await this.cacheService.DeleteAsync(split);
            }
            await this.cacheService.DeleteAsync(expense);
        }
        foreach (var expense in remoteData.Expenses)
        {
            await this.cacheService.SaveAsync(expense);
        }
        foreach (var split in remoteData.ExpenseSplits)
        {
            await this.cacheService.SaveAsync(split);
        }

        // Clear and re-add settlements
        var existingSettlements = await this.settlementService.GetGroupSettlementsAsync(groupId);
        foreach (var settlement in existingSettlements)
        {
            await this.cacheService.DeleteAsync(settlement);
        }
        foreach (var settlement in remoteData.Settlements)
        {
            await this.cacheService.SaveAsync(settlement);
        }

        metadata.Status = SyncStatus.Synced;
        metadata.Version = remoteData.Version;
        metadata.LocalLastModified = remoteData.LastModified;
        metadata.RemoteLastModified = remoteData.LastModified;
        metadata.LastSyncTime = DateTime.UtcNow;
        metadata.ErrorMessage = null;
        
        await this.SaveSyncMetadataAsync(metadata);
        this.loggingService.LogInfo($"Remote changes applied for group {groupId}");
    }

    private async Task DownloadRemoteGroupsAsync()
    {
        var remoteFiles = await this.googleDriveService.ListGroupFilesAsync();
        var localGroups = await this.groupService.GetUserGroupsAsync();
        var localGroupIds = new HashSet<Guid>(localGroups.Select(g => g.Id));

        foreach (var (fileId, groupId) in remoteFiles)
        {
            if (!localGroupIds.Contains(groupId))
            {
                try
                {
                    this.loggingService.LogInfo($"Downloading new remote group {groupId}");
                    var remoteData = await this.googleDriveService.DownloadGroupDataAsync(fileId);
                    
                    if (remoteData?.Group != null)
                    {
                        // Create local copy
                        await this.cacheService.SaveAsync(remoteData.Group);
                        
                        foreach (var member in remoteData.Members)
                        {
                            await this.cacheService.SaveAsync(member);
                        }
                        
                        foreach (var expense in remoteData.Expenses)
                        {
                            await this.cacheService.SaveAsync(expense);
                        }
                        
                        foreach (var split in remoteData.ExpenseSplits)
                        {
                            await this.cacheService.SaveAsync(split);
                        }
                        
                        foreach (var settlement in remoteData.Settlements)
                        {
                            await this.cacheService.SaveAsync(settlement);
                        }

                        // Create sync metadata
                        var metadata = new SyncMetadata
                        {
                            GroupId = groupId,
                            DriveFileId = fileId,
                            Status = SyncStatus.Synced,
                            Version = remoteData.Version,
                            LocalLastModified = remoteData.LastModified,
                            RemoteLastModified = remoteData.LastModified,
                            LastSyncTime = DateTime.UtcNow,
                        };
                        await this.SaveSyncMetadataAsync(metadata);
                        
                        this.loggingService.LogInfo($"Remote group {groupId} downloaded successfully");
                    }
                }
                catch (Exception ex)
                {
                    this.loggingService.LogError($"Failed to download remote group {groupId}: {ex.Message}");
                }
            }
        }
    }

    private async Task<SyncMetadata> GetOrCreateSyncMetadataAsync(Guid groupId)
    {
        var allMetadata = await this.cacheService.GetAllAsync<SyncMetadata>();
        var metadata = allMetadata.FirstOrDefault(m => m.GroupId == groupId);
        
        if (metadata == null)
        {
            metadata = new SyncMetadata
            {
                GroupId = groupId,
                Status = SyncStatus.NotSynced,
                LastSyncTime = DateTime.MinValue,
                LocalLastModified = DateTime.UtcNow,
                RemoteLastModified = DateTime.MinValue,
                Version = 0,
            };
        }
        
        return metadata;
    }

    private async Task SaveSyncMetadataAsync(SyncMetadata metadata)
    {
        await this.cacheService.SaveAsync(metadata);
    }

    /// <summary>
    /// Gets the Drive file ID for a synced group.
    /// </summary>
    public async Task<string?> GetGroupDriveFileIdAsync(string groupId)
    {
        var allMetadata = await this.cacheService.GetAllAsync<SyncMetadata>();
        var metadata = allMetadata.FirstOrDefault(m => m.GroupId.ToString() == groupId);
        return metadata?.DriveFileId;
    }

    /// <summary>
    /// Gets the last sync time.
    /// </summary>
    public DateTime? GetLastSyncTime()
    {
        var allMetadata = this.cacheService.GetAllAsync<SyncMetadata>().Result;
        var lastSync = allMetadata.OrderByDescending(m => m.LastSyncTime).FirstOrDefault();
        return lastSync?.LastSyncTime == DateTime.MinValue ? null : lastSync?.LastSyncTime;
    }

    /// <summary>
    /// Syncs a specific group with Google Drive (string overload).
    /// </summary>
    public async Task<SyncStatus> SyncGroupAsync(string groupId)
    {
        if (Guid.TryParse(groupId, out var guid))
        {
            await this.SyncGroupAsync(guid);
            var metadata = await this.GetSyncStatusAsync(guid);
            return metadata.Status;
        }
        
        throw new ArgumentException("Invalid group ID format", nameof(groupId));
    }
}
