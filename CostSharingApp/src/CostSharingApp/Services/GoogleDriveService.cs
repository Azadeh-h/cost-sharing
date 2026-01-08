
using System.Text;
using System.Text.Json;
using CostSharingApp.Models.GoogleSync;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;

namespace CostSharingApp.Services;
/// <summary>
/// Service for managing group data in Google Drive.
/// </summary>
public class GoogleDriveService : IGoogleDriveService
{
    private const string AppFolderName = "CostSharingApp";
    private const string GroupsFolderName = "groups";
    private readonly IGoogleAuthService googleAuthService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleDriveService"/> class.
    /// </summary>
    /// <param name="googleAuthService">Google authentication service.</param>
    public GoogleDriveService(IGoogleAuthService googleAuthService)
    {
        this.googleAuthService = googleAuthService;
    }

    /// <summary>
    /// Uploads group data to Google Drive.
    /// </summary>
    /// <param name="groupData">Group data to upload.</param>
    /// <param name="existingFileId">Existing file ID if updating.</param>
    /// <returns>The Drive file ID.</returns>
    public async Task<string> UploadGroupDataAsync(GroupSyncDto groupData, string? existingFileId = null)
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        var driveService = this.googleAuthService.GetDriveService();
        
        // Serialize group data to JSON
        var jsonData = JsonSerializer.Serialize(groupData, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));

        if (existingFileId != null)
        {
            // Update existing file
            var updateRequest = driveService.Files.Update(
                new Google.Apis.Drive.v3.Data.File(),
                existingFileId,
                stream,
                "application/json");
            
            var updatedFile = await updateRequest.UploadAsync();
            if (updatedFile.Status != Google.Apis.Upload.UploadStatus.Completed)
            {
                throw new Exception($"Upload failed: {updatedFile.Exception?.Message}");
            }

            return existingFileId;
        }
        else
        {
            // Create new file
            var groupsFolderId = await this.GetOrCreateGroupsFolderAsync(driveService);
            
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = $"{groupData.Group?.Id}.json",
                Parents = new List<string> { groupsFolderId },
                MimeType = "application/json",
            };

            var createRequest = driveService.Files.Create(
                fileMetadata,
                stream,
                "application/json");
            
            createRequest.Fields = "id";
            var uploadProgress = await createRequest.UploadAsync();
            
            if (uploadProgress.Status != Google.Apis.Upload.UploadStatus.Completed)
            {
                throw new Exception($"Upload failed: {uploadProgress.Exception?.Message}");
            }

            return createRequest.ResponseBody.Id;
        }
    }

    /// <summary>
    /// Downloads group data from Google Drive.
    /// </summary>
    /// <param name="fileId">The Drive file ID.</param>
    /// <returns>The group data.</returns>
    public async Task<GroupSyncDto?> DownloadGroupDataAsync(string fileId)
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        var driveService = this.googleAuthService.GetDriveService();
        
        var request = driveService.Files.Get(fileId);
        using var stream = new MemoryStream();
        
        await request.DownloadAsync(stream);
        stream.Position = 0;
        
        using var reader = new StreamReader(stream);
        var jsonData = await reader.ReadToEndAsync();
        
        return JsonSerializer.Deserialize<GroupSyncDto>(jsonData);
    }

    /// <summary>
    /// Lists all group files accessible to the user.
    /// </summary>
    /// <returns>List of file IDs and group IDs.</returns>
    public async Task<List<(string FileId, Guid GroupId)>> ListGroupFilesAsync()
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        var driveService = this.googleAuthService.GetDriveService();
        
        var groupsFolderId = await this.GetOrCreateGroupsFolderAsync(driveService);
        
        var request = driveService.Files.List();
        request.Q = $"'{groupsFolderId}' in parents and mimeType='application/json' and trashed=false";
        request.Fields = "files(id, name)";
        
        var result = await request.ExecuteAsync();
        var files = new List<(string FileId, Guid GroupId)>();
        
        foreach (var file in result.Files)
        {
            // Extract group ID from filename (format: {guid}.json)
            if (file.Name.EndsWith(".json") && 
                Guid.TryParse(file.Name.Replace(".json", string.Empty), out var groupId))
            {
                files.Add((file.Id, groupId));
            }
        }

        return files;
    }

    /// <summary>
    /// Shares a group file with another user.
    /// </summary>
    /// <param name="fileId">The Drive file ID.</param>
    /// <param name="emailAddress">Email address to share with.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ShareGroupFileAsync(string fileId, string emailAddress)
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        var driveService = this.googleAuthService.GetDriveService();
        
        var permission = new Permission
        {
            Type = "user",
            Role = "writer",
            EmailAddress = emailAddress,
        };

        await driveService.Permissions.Create(permission, fileId).ExecuteAsync();
    }

    /// <summary>
    /// Deletes a group file from Drive.
    /// </summary>
    /// <param name="fileId">The Drive file ID.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task DeleteGroupFileAsync(string fileId)
    {
        if (!this.googleAuthService.IsAuthenticated)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        var driveService = this.googleAuthService.GetDriveService();
        await driveService.Files.Delete(fileId).ExecuteAsync();
    }

    private async Task<string> GetOrCreateGroupsFolderAsync(DriveService driveService)
    {
        // Check if app folder exists
        var appFolderRequest = driveService.Files.List();
        appFolderRequest.Q = $"name='{AppFolderName}' and mimeType='application/vnd.google-apps.folder' and trashed=false";
        appFolderRequest.Fields = "files(id)";
        var appFolderResult = await appFolderRequest.ExecuteAsync();
        
        string appFolderId;
        if (appFolderResult.Files.Count == 0)
        {
            // Create app folder
            var appFolderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = AppFolderName,
                MimeType = "application/vnd.google-apps.folder",
            };
            var createdAppFolder = await driveService.Files.Create(appFolderMetadata).ExecuteAsync();
            appFolderId = createdAppFolder.Id;
        }
        else
        {
            appFolderId = appFolderResult.Files[0].Id;
        }

        // Check if groups folder exists
        var groupsFolderRequest = driveService.Files.List();
        groupsFolderRequest.Q = $"name='{GroupsFolderName}' and '{appFolderId}' in parents and mimeType='application/vnd.google-apps.folder' and trashed=false";
        groupsFolderRequest.Fields = "files(id)";
        var groupsFolderResult = await groupsFolderRequest.ExecuteAsync();
        
        if (groupsFolderResult.Files.Count == 0)
        {
            // Create groups folder
            var groupsFolderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = GroupsFolderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { appFolderId },
            };
            var createdGroupsFolder = await driveService.Files.Create(groupsFolderMetadata).ExecuteAsync();
            return createdGroupsFolder.Id;
        }
        else
        {
            return groupsFolderResult.Files[0].Id;
        }
    }
}
