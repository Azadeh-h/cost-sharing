using System.Timers;

namespace CostSharingApp.Services;

/// <summary>
/// Service for background synchronization with Google Drive.
/// </summary>
public class BackgroundSyncService : IDisposable
{
    private readonly ICacheService cacheService;
    private readonly IDriveService driveService;
    private readonly ILoggingService loggingService;
    private readonly System.Timers.Timer syncTimer;
    private bool isOnline = true;
    private bool isSyncing;

    /// <summary>
    /// Occurs when sync status changes.
    /// </summary>
    public event EventHandler<SyncStatusChangedEventArgs>? SyncStatusChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundSyncService"/> class.
    /// </summary>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="driveService">The drive service.</param>
    /// <param name="loggingService">The logging service.</param>
    public BackgroundSyncService(
        ICacheService cacheService,
        IDriveService driveService,
        ILoggingService loggingService)
    {
        this.cacheService = cacheService;
        this.driveService = driveService;
        this.loggingService = loggingService;

        // Sync every 5 minutes
        this.syncTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        this.syncTimer.Elapsed += this.OnSyncTimerElapsed;
        this.syncTimer.AutoReset = true;

        // Monitor connectivity
        Connectivity.ConnectivityChanged += this.OnConnectivityChanged;
        this.isOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }

    /// <summary>
    /// Gets a value indicating whether the device is online.
    /// </summary>
    public bool IsOnline => this.isOnline;

    /// <summary>
    /// Gets a value indicating whether sync is in progress.
    /// </summary>
    public bool IsSyncing => this.isSyncing;

    /// <summary>
    /// Gets the current sync status.
    /// </summary>
    public SyncStatus CurrentStatus
    {
        get
        {
            if (!this.isOnline)
            {
                return SyncStatus.Offline;
            }

            if (this.isSyncing)
            {
                return SyncStatus.Syncing;
            }

            return SyncStatus.Synced;
        }
    }

    /// <summary>
    /// Starts the background sync service.
    /// </summary>
    public void Start()
    {
        this.syncTimer.Start();
        this.loggingService.LogInfo("Background sync service started");
        
        // Perform initial sync if online
        if (this.isOnline)
        {
            _ = this.SyncNowAsync();
        }
    }

    /// <summary>
    /// Stops the background sync service.
    /// </summary>
    public void Stop()
    {
        this.syncTimer.Stop();
        this.loggingService.LogInfo("Background sync service stopped");
    }

    /// <summary>
    /// Triggers an immediate sync.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task SyncNowAsync()
    {
        if (!this.isOnline || this.isSyncing)
        {
            return;
        }

        try
        {
            this.isSyncing = true;
            this.NotifySyncStatusChanged();

            this.loggingService.LogInfo("Starting sync...");

            // In a real implementation, this would:
            // 1. Upload pending local changes to Drive
            // 2. Download remote changes from Drive
            // 3. Resolve any conflicts
            // 4. Update local cache

            // For now, just simulate sync delay
            await Task.Delay(1000);

            this.loggingService.LogInfo("Sync completed successfully");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Sync failed: {ex.Message}");
        }
        finally
        {
            this.isSyncing = false;
            this.NotifySyncStatusChanged();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.syncTimer?.Dispose();
        Connectivity.ConnectivityChanged -= this.OnConnectivityChanged;
        GC.SuppressFinalize(this);
    }

    private void OnSyncTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _ = this.SyncNowAsync();
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var wasOnline = this.isOnline;
        this.isOnline = e.NetworkAccess == NetworkAccess.Internet;

        if (wasOnline != this.isOnline)
        {
            this.loggingService.LogInfo($"Network status changed: {(this.isOnline ? "Online" : "Offline")}");
            this.NotifySyncStatusChanged();

            // Trigger sync when coming back online
            if (this.isOnline && !wasOnline)
            {
                _ = this.SyncNowAsync();
            }
        }
    }

    private void NotifySyncStatusChanged()
    {
        this.SyncStatusChanged?.Invoke(this, new SyncStatusChangedEventArgs(this.CurrentStatus));
    }
}

/// <summary>
/// Sync status enumeration.
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Device is offline.
    /// </summary>
    Offline,

    /// <summary>
    /// Sync in progress.
    /// </summary>
    Syncing,

    /// <summary>
    /// All data synced.
    /// </summary>
    Synced,

    /// <summary>
    /// Sync error occurred.
    /// </summary>
    Error
}

/// <summary>
/// Event args for sync status changes.
/// </summary>
public class SyncStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="status">The new sync status.</param>
    public SyncStatusChangedEventArgs(SyncStatus status)
    {
        this.Status = status;
    }

    /// <summary>
    /// Gets the current sync status.
    /// </summary>
    public SyncStatus Status { get; }
}
