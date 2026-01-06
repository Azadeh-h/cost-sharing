using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using System.Text;
using System.Text.Json;
using GoogleDriveService = Google.Apis.Drive.v3.DriveService;

namespace CostSharingApp.Services;

/// <summary>
/// Provides Google Drive file operations for peer-to-peer data storage.
/// </summary>
public class DriveService : IDriveService
{
    private readonly IDriveAuthService authService;
    private const string AppFolderName = "CostSharingApp";

    /// <summary>
    /// Initializes a new instance of the <see cref="DriveService"/> class.
    /// </summary>
    /// <param name="authService">Drive authentication service.</param>
    public DriveService(IDriveAuthService authService)
    {
        this.authService = authService;
    }

    /// <summary>
    /// Gets or creates the app's root folder in Google Drive.
    /// </summary>
    /// <returns>Folder ID.</returns>
    public async Task<string?> GetOrCreateAppFolderAsync()
    {
        var service = this.authService.GetDriveService();
        if (service == null)
        {
            return null;
        }

        // Search for existing folder
        var listRequest = service.Files.List();
        listRequest.Q = $"name='{AppFolderName}' and mimeType='application/vnd.google-apps.folder' and trashed=false";
        listRequest.Fields = "files(id, name)";

        var files = await listRequest.ExecuteAsync();
        if (files.Files.Count > 0)
        {
            return files.Files[0].Id;
        }

        // Create new folder
        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = AppFolderName,
            MimeType = "application/vnd.google-apps.folder"
        };

        var request = service.Files.Create(folderMetadata);
        request.Fields = "id";
        var folder = await request.ExecuteAsync();

        return folder.Id;
    }

    /// <summary>
    /// Saves JSON data to a file in Drive.
    /// </summary>
    /// <typeparam name="T">Data type.</typeparam>
    /// <param name="fileName">File name.</param>
    /// <param name="data">Data to save.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SaveFileAsync<T>(string fileName, T data)
    {
        var service = this.authService.GetDriveService();
        if (service == null)
        {
            return false;
        }

        try
        {
            var folderId = await this.GetOrCreateAppFolderAsync();
            if (folderId == null)
            {
                return false;
            }

            var json = JsonSerializer.Serialize(data);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Check if file exists
            var existingFileId = await this.FindFileAsync(fileName, folderId);

            if (existingFileId != null)
            {
                // Update existing file
                var updateRequest = service.Files.Update(
                    new Google.Apis.Drive.v3.Data.File(),
                    existingFileId,
                    stream,
                    "application/json");
                await updateRequest.UploadAsync();
            }
            else
            {
                // Create new file
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new List<string> { folderId }
                };

                var createRequest = service.Files.Create(
                    fileMetadata,
                    stream,
                    "application/json");
                await createRequest.UploadAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Drive save failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads JSON data from a file in Drive.
    /// </summary>
    /// <typeparam name="T">Data type.</typeparam>
    /// <param name="fileName">File name.</param>
    /// <returns>Deserialized data or default.</returns>
    public async Task<T?> LoadFileAsync<T>(string fileName)
    {
        var service = this.authService.GetDriveService();
        if (service == null)
        {
            return default;
        }

        try
        {
            var folderId = await this.GetOrCreateAppFolderAsync();
            if (folderId == null)
            {
                return default;
            }

            var fileId = await this.FindFileAsync(fileName, folderId);
            if (fileId == null)
            {
                return default;
            }

            var request = service.Files.Get(fileId);
            var stream = new MemoryStream();
            await request.DownloadAsync(stream);

            stream.Position = 0;
            var json = await new StreamReader(stream).ReadToEndAsync();

            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Drive load failed: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Finds file ID by name in folder.
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <param name="folderId">Parent folder ID.</param>
    /// <returns>File ID or null.</returns>
    private async Task<string?> FindFileAsync(string fileName, string folderId)
    {
        var service = this.authService.GetDriveService();
        if (service == null)
        {
            return null;
        }

        var listRequest = service.Files.List();
        listRequest.Q = $"name='{fileName}' and '{folderId}' in parents and trashed=false";
        listRequest.Fields = "files(id)";

        var files = await listRequest.ExecuteAsync();
        return files.Files.Count > 0 ? files.Files[0].Id : null;
    }
}

/// <summary>
/// Interface for Google Drive operations.
/// </summary>
public interface IDriveService
{
    /// <summary>
    /// Gets or creates app folder.
    /// </summary>
    /// <returns>Folder ID.</returns>
    Task<string?> GetOrCreateAppFolderAsync();

    /// <summary>
    /// Saves data to Drive.
    /// </summary>
    /// <typeparam name="T">Data type.</typeparam>
    /// <param name="fileName">File name.</param>
    /// <param name="data">Data.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SaveFileAsync<T>(string fileName, T data);

    /// <summary>
    /// Loads data from Drive.
    /// </summary>
    /// <typeparam name="T">Data type.</typeparam>
    /// <param name="fileName">File name.</param>
    /// <returns>Data or default.</returns>
    Task<T?> LoadFileAsync<T>(string fileName);
}
