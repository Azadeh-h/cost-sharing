// <copyright file="DriveSyncService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;
using System.Text.Json;
using CostSharing.Core.Models;
using CostSharing.Core.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;

namespace CostSharingApp.Services;

/// <summary>
/// Data container for group sync.
/// </summary>
public class GroupSyncData
{
    /// <summary>
    /// Gets or sets the group.
    /// </summary>
    public Group? Group { get; set; }

    /// <summary>
    /// Gets or sets the group members.
    /// </summary>
    public List<GroupMember> Members { get; set; } = new();

    /// <summary>
    /// Gets or sets the users in the group.
    /// </summary>
    public List<CostSharing.Core.Models.User> Users { get; set; } = new();

    /// <summary>
    /// Gets or sets the expenses.
    /// </summary>
    public List<Expense> Expenses { get; set; } = new();

    /// <summary>
    /// Gets or sets the expense splits.
    /// </summary>
    public List<ExpenseSplit> ExpenseSplits { get; set; } = new();

    /// <summary>
    /// Gets or sets the settlements.
    /// </summary>
    public List<Settlement> Settlements { get; set; } = new();

    /// <summary>
    /// Gets or sets the sync timestamp.
    /// </summary>
    public DateTime SyncedAt { get; set; }
}

/// <summary>
/// Service for synchronizing group data with Google Drive.
/// </summary>
public class DriveSyncService : IDriveSyncService
{
    /// <summary>
    /// The name of the parent folder that contains all CostSharing group folders.
    /// </summary>
    private const string ParentFolderName = "Costsharing-Groups";

    private readonly IAuthService authService;
    private readonly IGroupService groupService;
    private readonly IExpenseService expenseService;
    private readonly ISettlementService settlementService;
    private readonly ICacheService cacheService;
    private readonly ILoggingService loggingService;
    private readonly IDriveAuthService driveAuthService;
    private readonly DriveErrorHandler errorHandler;
    private DriveService? driveService;
    private string? parentFolderId;

    /// <summary>
    /// Initializes a new instance of the <see cref="DriveSyncService"/> class.
    /// </summary>
    /// <param name="authService">Authentication service.</param>
    /// <param name="groupService">Group service.</param>
    /// <param name="expenseService">Expense service.</param>
    /// <param name="settlementService">Settlement service.</param>
    /// <param name="cacheService">Cache service.</param>
    /// <param name="loggingService">Logging service.</param>
    /// <param name="driveAuthService">Drive auth service.</param>
    /// <param name="errorHandler">Drive error handler.</param>
    public DriveSyncService(
        IAuthService authService,
        IGroupService groupService,
        IExpenseService expenseService,
        ISettlementService settlementService,
        ICacheService cacheService,
        ILoggingService loggingService,
        IDriveAuthService driveAuthService,
        DriveErrorHandler errorHandler)
    {
        this.authService = authService;
        this.groupService = groupService;
        this.expenseService = expenseService;
        this.settlementService = settlementService;
        this.cacheService = cacheService;
        this.loggingService = loggingService;
        this.driveAuthService = driveAuthService;
        this.errorHandler = errorHandler;
    }

    /// <inheritdoc/>
    public async Task<string> CreateGroupFolderAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            this.loggingService.LogInfo($"Creating Drive folder for group {groupId}");

            await this.InitializeDriveServiceAsync(userId, cancellationToken);

            if (this.driveService == null)
            {
                throw new InvalidOperationException("Drive service not initialized");
            }

            var group = await this.groupService.GetGroupAsync(groupId);
            if (group == null)
            {
                throw new ArgumentException($"Group {groupId} not found");
            }

            // Get or create the parent "Costsharing-groups" folder
            var parentId = await this.GetOrCreateParentFolderAsync(cancellationToken);

            // Create group folder metadata inside the parent folder
            var folderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = $"{group.Name}",
                MimeType = "application/vnd.google-apps.folder",
                Description = $"Shared folder for {group.Name} cost sharing data",
                Parents = new List<string> { parentId },
            };

            // Create folder
            var request = this.driveService.Files.Create(folderMetadata);
            request.Fields = "id, name, webViewLink";
            var folder = await this.errorHandler.ExecuteWithRetryAsync(
                async () => await request.ExecuteAsync(cancellationToken),
                cancellationToken);

            this.loggingService.LogInfo($"Created Drive folder {folder.Id} for group {groupId} inside {ParentFolderName}");

            // Save folder ID to group
            group.DriveFolderId = folder.Id;
            await this.groupService.UpdateGroupAsync(group);

            // Share folder with all group members
            await this.ShareFolderWithMembersAsync(folder.Id, groupId, userId, cancellationToken);

            return folder.Id;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to create Drive folder for group {groupId}", ex);
            throw;
        }
    }

    /// <summary>
    /// Gets or creates the parent "Costsharing-groups" folder in Google Drive.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The folder ID of the parent folder.</returns>
    private async Task<string> GetOrCreateParentFolderAsync(CancellationToken cancellationToken = default)
    {
        // Return cached parent folder ID if available
        if (!string.IsNullOrEmpty(this.parentFolderId))
        {
            return this.parentFolderId;
        }

        if (this.driveService == null)
        {
            throw new InvalidOperationException("Drive service not initialized");
        }

        // Search for existing parent folder
        var searchRequest = this.driveService.Files.List();
        searchRequest.Q = $"name = '{ParentFolderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
        searchRequest.Fields = "files(id, name)";

        var searchResult = await this.errorHandler.ExecuteWithRetryAsync(
            async () => await searchRequest.ExecuteAsync(cancellationToken),
            cancellationToken);

        if (searchResult.Files != null && searchResult.Files.Count > 0)
        {
            this.parentFolderId = searchResult.Files[0].Id;
            this.loggingService.LogInfo($"Found existing parent folder: {this.parentFolderId}");
            return this.parentFolderId;
        }

        // Create parent folder if it doesn't exist
        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = ParentFolderName,
            MimeType = "application/vnd.google-apps.folder",
            Description = "Parent folder containing all CostSharing group folders",
        };

        var createRequest = this.driveService.Files.Create(folderMetadata);
        createRequest.Fields = "id, name";
        var folder = await this.errorHandler.ExecuteWithRetryAsync(
            async () => await createRequest.ExecuteAsync(cancellationToken),
            cancellationToken);

        this.parentFolderId = folder.Id;
        this.loggingService.LogInfo($"Created parent folder: {this.parentFolderId}");
        return this.parentFolderId;
    }

    /// <summary>
    /// Verifies that a folder still exists in Google Drive.
    /// </summary>
    /// <param name="folderId">The folder ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the folder exists and is not trashed, false otherwise.</returns>
    private async Task<bool> VerifyFolderExistsAsync(string folderId, CancellationToken cancellationToken = default)
    {
        if (this.driveService == null)
        {
            return false;
        }

        try
        {
            var request = this.driveService.Files.Get(folderId);
            request.Fields = "id, trashed";
            var file = await request.ExecuteAsync(cancellationToken);
            return file != null && file.Trashed != true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            this.loggingService.LogInfo($"Folder {folderId} not found in Drive");
            return false;
        }
        catch (Exception ex)
        {
            this.loggingService.LogWarning($"Error checking folder existence: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task SetFolderPermissionsAsync(
        string folderId,
        IEnumerable<string> memberEmails,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            this.loggingService.LogInfo($"Setting permissions for folder {folderId}");

            await this.InitializeDriveServiceAsync(userId, cancellationToken);

            if (this.driveService == null)
            {
                throw new InvalidOperationException("Drive service not initialized");
            }

            foreach (var email in memberEmails)
            {
                var permission = new Permission
                {
                    Type = "user",
                    Role = "writer",
                    EmailAddress = email,
                };

                var permissionRequest = this.driveService.Permissions.Create(permission, folderId);
                permissionRequest.SendNotificationEmail = true;
                permissionRequest.EmailMessage = "You've been added to a CostSharing group. You now have access to the shared Drive folder.";

                await this.errorHandler.ExecuteWithRetryAsync(
                    async () => await permissionRequest.ExecuteAsync(cancellationToken),
                    cancellationToken);

                this.loggingService.LogInfo($"Granted access to {email} for folder {folderId}");
            }
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to set permissions for folder {folderId}", ex);
            throw;
        }
    }

    /// <summary>
    /// Shares a folder with all members of a group.
    /// </summary>
    /// <param name="folderId">The folder ID to share.</param>
    /// <param name="groupId">The group ID.</param>
    /// <param name="currentUserId">The current user ID (owner, won't be shared with).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task for async operation.</returns>
    private async Task ShareFolderWithMembersAsync(string folderId, Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await this.groupService.GetGroupMembersAsync(groupId);
            var memberEmails = new List<string>();

            foreach (var member in members)
            {
                // Don't share with the current user (they already own it)
                if (member.UserId == currentUserId)
                {
                    continue;
                }

                var user = await this.authService.GetUserByIdAsync(member.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email) && user.Email.Contains("@"))
                {
                    // Only add real email addresses (not device-generated ones)
                    if (!user.Email.EndsWith("@device.local"))
                    {
                        memberEmails.Add(user.Email);
                    }
                }
            }

            if (memberEmails.Any())
            {
                this.loggingService.LogInfo($"Sharing folder {folderId} with {memberEmails.Count} members: {string.Join(", ", memberEmails)}");
                await this.SetFolderPermissionsAsync(folderId, memberEmails, currentUserId, cancellationToken);
            }
            else
            {
                this.loggingService.LogInfo($"No members with valid emails to share folder {folderId} with");
            }
        }
        catch (Exception ex)
        {
            // Don't fail the whole sync if sharing fails - just log the error
            this.loggingService.LogError($"Failed to share folder {folderId} with members", ex);
        }
    }

    /// <inheritdoc/>
    public async Task UploadGroupDataAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            this.loggingService.LogInfo($"Uploading data for group {groupId}");

            await this.InitializeDriveServiceAsync(userId, cancellationToken);

            if (this.driveService == null)
            {
                throw new InvalidOperationException("Drive service not initialized");
            }

            var group = await this.groupService.GetGroupAsync(groupId);
            if (group == null)
            {
                throw new ArgumentException($"Group {groupId} not found");
            }

            // Verify the folder still exists in Drive, recreate if deleted
            if (!string.IsNullOrEmpty(group.DriveFolderId))
            {
                var folderExists = await this.VerifyFolderExistsAsync(group.DriveFolderId, cancellationToken);
                if (!folderExists)
                {
                    this.loggingService.LogWarning($"Drive folder {group.DriveFolderId} no longer exists, recreating...");
                    group.DriveFolderId = null;
                }
            }

            // Create folder if it doesn't exist
            if (string.IsNullOrEmpty(group.DriveFolderId))
            {
                var folderId = await this.CreateGroupFolderAsync(groupId, userId, cancellationToken);
                group = await this.groupService.GetGroupAsync(groupId); // Refresh group after folder creation
            }

            // Get all data for the group
            var members = await this.groupService.GetGroupMembersAsync(groupId);
            var expenses = await this.expenseService.GetGroupExpensesAsync(groupId);
            var settlements = await this.settlementService.GetGroupSettlementsAsync(groupId);

            // Get expense splits for all expenses
            var allSplits = new List<ExpenseSplit>();
            foreach (var expense in expenses)
            {
                var splits = await this.expenseService.GetExpenseSplitsAsync(expense.Id);
                allSplits.AddRange(splits);
            }

            // Get all users involved in the group
            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            var users = new List<CostSharing.Core.Models.User>();
            foreach (var uid in userIds)
            {
                var user = await this.authService.GetUserByIdAsync(uid);
                if (user != null)
                {
                    users.Add(user);
                }
            }

            // Create sync data container
            var groupData = new GroupSyncData
            {
                Group = group,
                Members = members.ToList(),
                Users = users,
                Expenses = expenses,
                ExpenseSplits = allSplits,
                Settlements = settlements.ToList(),
                SyncedAt = DateTime.UtcNow,
            };

            var json = JsonSerializer.Serialize(groupData, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            // Check if file already exists in the folder
            var listRequest = this.driveService.Files.List();
            listRequest.Q = $"name = 'group-data.json' and '{group.DriveFolderId}' in parents and trashed = false";
            listRequest.Fields = "files(id, name)";
            var existingFiles = await this.errorHandler.ExecuteWithRetryAsync(
                async () => await listRequest.ExecuteAsync(cancellationToken),
                cancellationToken);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            if (existingFiles.Files != null && existingFiles.Files.Count > 0)
            {
                // Update existing file
                var existingFileId = existingFiles.Files[0].Id;
                var updateRequest = this.driveService.Files.Update(
                    new Google.Apis.Drive.v3.Data.File { Name = "group-data.json" },
                    existingFileId,
                    stream,
                    "application/json");
                updateRequest.Fields = "id, name, modifiedTime";

                await this.errorHandler.ExecuteWithRetryAsync(
                    async () => await updateRequest.UploadAsync(cancellationToken),
                    cancellationToken);

                this.loggingService.LogInfo($"Updated existing group-data.json for group {groupId}");
            }
            else
            {
                // Create new file
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = "group-data.json",
                    Parents = new List<string> { group.DriveFolderId },
                };

                var createRequest = this.driveService.Files.Create(fileMetadata, stream, "application/json");
                createRequest.Fields = "id, name, modifiedTime";

                await this.errorHandler.ExecuteWithRetryAsync(
                    async () => await createRequest.UploadAsync(cancellationToken),
                    cancellationToken);

                this.loggingService.LogInfo($"Created new group-data.json for group {groupId}");
            }

            // Update last sync timestamp
            group.LastSyncTimestamp = DateTime.UtcNow;
            await this.groupService.UpdateGroupAsync(group);

            this.loggingService.LogInfo($"Uploaded data for group {groupId}");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to upload data for group {groupId}", ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DownloadGroupDataAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            this.loggingService.LogInfo($"Downloading data for group {groupId}");

            await this.InitializeDriveServiceAsync(userId, cancellationToken);

            if (this.driveService == null)
            {
                throw new InvalidOperationException("Drive service not initialized");
            }

            var group = await this.groupService.GetGroupAsync(groupId);
            if (group == null || string.IsNullOrEmpty(group.DriveFolderId))
            {
                throw new ArgumentException($"Group {groupId} not found or sync not enabled");
            }

            // Find group-data.json file
            var listRequest = this.driveService.Files.List();
            listRequest.Q = $"name='group-data.json' and '{group.DriveFolderId}' in parents and trashed=false";
            listRequest.Fields = "files(id, name, modifiedTime)";

            var files = await this.errorHandler.ExecuteWithRetryAsync(
                async () => await listRequest.ExecuteAsync(cancellationToken),
                cancellationToken);

            if (files.Files == null || files.Files.Count == 0)
            {
                this.loggingService.LogWarning($"No data file found for group {groupId}");
                return;
            }

            var fileId = files.Files[0].Id;

            // Download file content
            var downloadRequest = this.driveService.Files.Get(fileId);
            using var stream = new MemoryStream();
            await this.errorHandler.ExecuteWithRetryAsync(
                async () =>
                {
                    await downloadRequest.DownloadAsync(stream, cancellationToken);
                    return true;
                },
                cancellationToken);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            // Deserialize and merge the data
            var syncData = JsonSerializer.Deserialize<GroupSyncData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (syncData != null)
            {
                await this.MergeGroupDataAsync(syncData);
                this.loggingService.LogInfo($"Successfully merged data for group {groupId}");
            }

            // Update last sync timestamp
            group.LastSyncTimestamp = DateTime.UtcNow;
            await this.groupService.UpdateGroupAsync(group);
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to download data for group {groupId}", ex);
            throw;
        }
    }

    /// <summary>
    /// Merges downloaded sync data into the local database.
    /// </summary>
    /// <param name="syncData">The sync data to merge.</param>
    /// <returns>Task for async operation.</returns>
    private async Task MergeGroupDataAsync(GroupSyncData syncData)
    {
        // Merge users first (needed for foreign key references)
        foreach (var user in syncData.Users)
        {
            var existingUser = await this.authService.GetUserByIdAsync(user.Id);
            if (existingUser == null)
            {
                // Add new user
                await this.cacheService.SaveAsync(user);
                this.loggingService.LogInfo($"Added user {user.Name} from sync");
            }
        }

        // Merge group members
        foreach (var member in syncData.Members)
        {
            await this.cacheService.SaveAsync(member);
        }

        // Merge expenses (use last-write-wins for simplicity)
        foreach (var expense in syncData.Expenses)
        {
            var existingExpense = await this.expenseService.GetExpenseAsync(expense.Id);
            if (existingExpense == null || expense.ModifiedTimestamp > existingExpense.ModifiedTimestamp)
            {
                await this.cacheService.SaveAsync(expense);
                this.loggingService.LogInfo($"Merged expense {expense.Description}");
            }
        }

        // Merge expense splits
        foreach (var split in syncData.ExpenseSplits)
        {
            await this.cacheService.SaveAsync(split);
        }

        // Merge settlements
        foreach (var settlement in syncData.Settlements)
        {
            await this.cacheService.SaveAsync(settlement);
        }

        this.loggingService.LogInfo($"Merge completed: {syncData.Users.Count} users, {syncData.Expenses.Count} expenses, {syncData.Settlements.Count} settlements");
    }

    /// <inheritdoc/>
    public async Task<bool> CheckForChangesAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await this.InitializeDriveServiceAsync(userId, cancellationToken);

            if (this.driveService == null)
            {
                return false;
            }

            var group = await this.groupService.GetGroupAsync(groupId);
            if (group == null || string.IsNullOrEmpty(group.DriveFolderId))
            {
                return false;
            }

            // Check if data file has been modified since last sync
            var listRequest = this.driveService.Files.List();
            listRequest.Q = $"name='group-data.json' and '{group.DriveFolderId}' in parents and trashed=false";
            listRequest.Fields = "files(id, modifiedTime)";

            var files = await this.errorHandler.ExecuteWithRetryAsync(
                async () => await listRequest.ExecuteAsync(cancellationToken),
                cancellationToken);

            if (files.Files == null || files.Files.Count == 0)
            {
                return false;
            }

            var fileModifiedTime = files.Files[0].ModifiedTime;
            var hasChanges = group.LastSyncTimestamp == null ||
                           (fileModifiedTime.HasValue && fileModifiedTime.Value > group.LastSyncTimestamp.Value);

            return hasChanges;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to check for changes for group {groupId}", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task StartPeriodicSyncAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
    {
        this.loggingService.LogInfo($"Starting periodic sync for group {groupId}");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check for changes every 2 minutes
                await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);

                if (await this.CheckForChangesAsync(groupId, userId, cancellationToken))
                {
                    this.loggingService.LogInfo($"Changes detected for group {groupId}, downloading...");
                    await this.DownloadGroupDataAsync(groupId, userId, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            this.loggingService.LogInfo($"Periodic sync stopped for group {groupId}");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Error in periodic sync for group {groupId}", ex);
        }
    }

    private async Task InitializeDriveServiceAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Always get a fresh/valid token (this handles refresh if needed)
        var accessToken = await this.driveAuthService.GetAccessTokenAsync(userId, cancellationToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("User not authorized for Drive access. Please re-authorize in Settings.");
        }

        var credential = GoogleCredential.FromAccessToken(accessToken);

        this.driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CostSharing",
        });
    }

    /// <inheritdoc/>
    public async Task<int> DiscoverSharedGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            this.loggingService.LogInfo($"Discovering shared groups for user {userId}");

            await this.InitializeDriveServiceAsync(userId, cancellationToken);

            if (this.driveService == null)
            {
                throw new InvalidOperationException("Drive service not initialized");
            }

            // Search for "Costsharing-Groups" folders shared with me
            var searchRequest = this.driveService.Files.List();
            searchRequest.Q = $"name = '{ParentFolderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            searchRequest.Fields = "files(id, name, owners)";
            searchRequest.Spaces = "drive";
            searchRequest.IncludeItemsFromAllDrives = true;
            searchRequest.SupportsAllDrives = true;

            var parentFolders = await this.errorHandler.ExecuteWithRetryAsync(
                async () => await searchRequest.ExecuteAsync(cancellationToken),
                cancellationToken);

            int importedCount = 0;

            if (parentFolders.Files == null || parentFolders.Files.Count == 0)
            {
                this.loggingService.LogInfo("No Costsharing-Groups folders found");
                return 0;
            }

            // For each parent folder, search for group subfolders
            foreach (var parentFolder in parentFolders.Files)
            {
                this.loggingService.LogInfo($"Searching in parent folder: {parentFolder.Id}");

                var groupFoldersRequest = this.driveService.Files.List();
                groupFoldersRequest.Q = $"'{parentFolder.Id}' in parents and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
                groupFoldersRequest.Fields = "files(id, name)";

                var groupFolders = await this.errorHandler.ExecuteWithRetryAsync(
                    async () => await groupFoldersRequest.ExecuteAsync(cancellationToken),
                    cancellationToken);

                if (groupFolders.Files == null)
                {
                    continue;
                }

                foreach (var groupFolder in groupFolders.Files)
                {
                    // Check if this folder has a group-data.json file
                    var dataFileRequest = this.driveService.Files.List();
                    dataFileRequest.Q = $"name = 'group-data.json' and '{groupFolder.Id}' in parents and trashed = false";
                    dataFileRequest.Fields = "files(id, name)";

                    var dataFiles = await this.errorHandler.ExecuteWithRetryAsync(
                        async () => await dataFileRequest.ExecuteAsync(cancellationToken),
                        cancellationToken);

                    if (dataFiles.Files != null && dataFiles.Files.Count > 0)
                    {
                        // Download and import the group data
                        var imported = await this.ImportGroupFromDriveAsync(
                            groupFolder.Id,
                            dataFiles.Files[0].Id,
                            userId,
                            cancellationToken);

                        if (imported)
                        {
                            importedCount++;
                        }
                    }
                }
            }

            this.loggingService.LogInfo($"Discovered and imported {importedCount} groups");
            return importedCount;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to discover shared groups for user {userId}", ex);
            throw;
        }
    }

    /// <summary>
    /// Imports a group from a Google Drive folder.
    /// </summary>
    private async Task<bool> ImportGroupFromDriveAsync(
        string folderId,
        string dataFileId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (this.driveService == null)
            {
                return false;
            }

            // Download the data file
            var downloadRequest = this.driveService.Files.Get(dataFileId);
            using var stream = new MemoryStream();
            await this.errorHandler.ExecuteWithRetryAsync(
                async () =>
                {
                    await downloadRequest.DownloadAsync(stream, cancellationToken);
                    return true;
                },
                cancellationToken);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var syncData = JsonSerializer.Deserialize<GroupSyncData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (syncData?.Group == null)
            {
                this.loggingService.LogWarning($"Invalid group data in folder {folderId}");
                return false;
            }

            // Check if this group already exists locally
            var existingGroup = await this.groupService.GetGroupAsync(syncData.Group.Id);
            if (existingGroup != null)
            {
                this.loggingService.LogInfo($"Group {syncData.Group.Name} already exists locally, updating...");
            }
            else
            {
                this.loggingService.LogInfo($"Importing new group: {syncData.Group.Name}");
            }

            // Update the DriveFolderId to point to the correct folder
            syncData.Group.DriveFolderId = folderId;

            // Merge all the data
            await this.MergeGroupDataAsync(syncData);

            // Also save the group with the folder ID
            await this.cacheService.SaveAsync(syncData.Group);

            this.loggingService.LogInfo($"Successfully imported group: {syncData.Group.Name}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to import group from folder {folderId}", ex);
            return false;
        }
    }
}
