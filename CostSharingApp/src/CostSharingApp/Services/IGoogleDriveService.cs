
using CostSharingApp.Models.GoogleSync;

namespace CostSharingApp.Services;
/// <summary>
/// Interface for Google Drive service.
/// </summary>
public interface IGoogleDriveService
{
    /// <summary>
    /// Uploads group data to Google Drive.
    /// </summary>
    /// <param name="groupData">Group data to upload.</param>
    /// <param name="existingFileId">Existing file ID if updating.</param>
    /// <returns>The Drive file ID.</returns>
    Task<string> UploadGroupDataAsync(GroupSyncDto groupData, string? existingFileId = null);

    /// <summary>
    /// Downloads group data from Google Drive.
    /// </summary>
    /// <param name="fileId">The Drive file ID.</param>
    /// <returns>The group data.</returns>
    Task<GroupSyncDto?> DownloadGroupDataAsync(string fileId);

    /// <summary>
    /// Lists all group files accessible to the user.
    /// </summary>
    /// <returns>List of file IDs and group IDs.</returns>
    Task<List<(string FileId, Guid GroupId)>> ListGroupFilesAsync();

    /// <summary>
    /// Shares a group file with another user.
    /// </summary>
    /// <param name="fileId">The Drive file ID.</param>
    /// <param name="emailAddress">Email address to share with.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ShareGroupFileAsync(string fileId, string emailAddress);

    /// <summary>
    /// Deletes a group file from Drive.
    /// </summary>
    /// <param name="fileId">The Drive file ID.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DeleteGroupFileAsync(string fileId);
}
