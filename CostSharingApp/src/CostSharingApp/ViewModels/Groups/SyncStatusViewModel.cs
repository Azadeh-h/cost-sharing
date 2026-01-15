// <copyright file="SyncStatusViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CostSharing.Core.Services;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels.Groups;

/// <summary>
/// ViewModel for sync status display and control.
/// </summary>
public partial class SyncStatusViewModel : BaseViewModel
{
    private readonly IDriveSyncService driveSyncService;
    private readonly IAuthService authService;
    private readonly ILoggingService loggingService;

    [ObservableProperty]
    private Guid groupId;

    [ObservableProperty]
    private bool isSyncEnabled;

    [ObservableProperty]
    private string syncStatusText = "Not synced";

    [ObservableProperty]
    private Color syncStatusColor = Colors.Gray;

    [ObservableProperty]
    private string? lastSyncTime;

    [ObservableProperty]
    private bool isSyncing;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncStatusViewModel"/> class.
    /// </summary>
    /// <param name="driveSyncService">Drive sync service.</param>
    /// <param name="authService">Authentication service.</param>
    /// <param name="loggingService">Logging service.</param>
    public SyncStatusViewModel(
        IDriveSyncService driveSyncService,
        IAuthService authService,
        ILoggingService loggingService)
    {
        this.driveSyncService = driveSyncService;
        this.authService = authService;
        this.loggingService = loggingService;
    }

    /// <summary>
    /// Command to manually trigger sync.
    /// </summary>
    /// <returns>Task.</returns>
    [RelayCommand]
    private async Task SyncNowAsync()
    {
        if (this.IsSyncing)
        {
            return;
        }

        try
        {
            this.IsSyncing = true;
            this.SyncStatusText = "Syncing...";
            this.SyncStatusColor = Colors.Orange;

            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                throw new InvalidOperationException("User not authenticated");
            }

            // Upload local changes
            await this.driveSyncService.UploadGroupDataAsync(this.GroupId, currentUser.Id);

            // Download remote changes
            await this.driveSyncService.DownloadGroupDataAsync(this.GroupId, currentUser.Id);

            this.SyncStatusText = "Synced";
            this.SyncStatusColor = Colors.Green;
            this.LastSyncTime = DateTime.Now.ToString("HH:mm");

            this.loggingService.LogInfo($"Manual sync completed for group {this.GroupId}");
        }
        catch (Exception ex)
        {
            this.SyncStatusText = "Sync failed";
            this.SyncStatusColor = Colors.Red;
            this.loggingService.LogError($"Manual sync failed for group {this.GroupId}", ex);
        }
        finally
        {
            this.IsSyncing = false;
        }
    }

    /// <summary>
    /// Updates the sync status display.
    /// </summary>
    /// <param name="status">Status text.</param>
    /// <param name="color">Status color.</param>
    public void UpdateStatus(string status, Color color)
    {
        this.SyncStatusText = status;
        this.SyncStatusColor = color;
    }
}
