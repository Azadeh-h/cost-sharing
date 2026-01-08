using System.ComponentModel;
using System.Runtime.CompilerServices;
using CostSharingApp.Services;
using CostSharingApp.Models;

namespace CostSharingApp.ViewModels;

public class SyncStatusViewModel : INotifyPropertyChanged
{
    private readonly IGoogleAuthService googleAuthService;
    private readonly IGoogleSyncService googleSyncService;
    private bool isSignedIn;
    private bool isSyncing;
    private string lastSyncTime;
    private string syncStatusText;
    private Color syncStatusColor;

    public SyncStatusViewModel(IGoogleAuthService googleAuthService, IGoogleSyncService googleSyncService)
    {
        this.googleAuthService = googleAuthService;
        this.googleSyncService = googleSyncService;
        
        this.syncStatusColor = Colors.Gray;
        this.syncStatusText = "Not connected";
        this.lastSyncTime = "Never";
        
        this.SyncNowCommand = new Command(async () => await this.SyncNowAsync(), () => this.IsSignedIn && !this.IsSyncing);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public bool IsSignedIn
    {
        get => this.isSignedIn;
        set
        {
            if (this.isSignedIn != value)
            {
                this.isSignedIn = value;
                this.OnPropertyChanged();
                ((Command)this.SyncNowCommand).ChangeCanExecute();
            }
        }
    }

    public bool IsSyncing
    {
        get => this.isSyncing;
        set
        {
            if (this.isSyncing != value)
            {
                this.isSyncing = value;
                this.OnPropertyChanged();
                ((Command)this.SyncNowCommand).ChangeCanExecute();
            }
        }
    }

    public string LastSyncTime
    {
        get => this.lastSyncTime;
        set
        {
            if (this.lastSyncTime != value)
            {
                this.lastSyncTime = value;
                this.OnPropertyChanged();
            }
        }
    }

    public string SyncStatusText
    {
        get => this.syncStatusText;
        set
        {
            if (this.syncStatusText != value)
            {
                this.syncStatusText = value;
                this.OnPropertyChanged();
            }
        }
    }

    public Color SyncStatusColor
    {
        get => this.syncStatusColor;
        set
        {
            if (this.syncStatusColor != value)
            {
                this.syncStatusColor = value;
                this.OnPropertyChanged();
            }
        }
    }

    public Command SyncNowCommand { get; }

    public async Task InitializeAsync()
    {
        await this.googleAuthService.InitializeAsync();
        this.IsSignedIn = this.googleAuthService.IsAuthenticated;
        
        if (this.IsSignedIn)
        {
            this.SyncStatusText = "Connected";
            this.SyncStatusColor = Colors.Green;
        }
        else
        {
            this.SyncStatusText = "Not connected";
            this.SyncStatusColor = Colors.Gray;
        }
        
        this.UpdateLastSyncTime();
    }

    public void UpdateLastSyncTime()
    {
        var lastSync = this.googleSyncService.GetLastSyncTime();
        
        if (lastSync == null)
        {
            this.LastSyncTime = "Never";
            return;
        }
        
        var timeSpan = DateTime.UtcNow - lastSync.Value;
        
        if (timeSpan.TotalMinutes < 1)
        {
            this.LastSyncTime = "Just now";
        }
        else if (timeSpan.TotalMinutes < 60)
        {
            this.LastSyncTime = $"{(int)timeSpan.TotalMinutes} min ago";
        }
        else if (timeSpan.TotalHours < 24)
        {
            this.LastSyncTime = $"{(int)timeSpan.TotalHours} hours ago";
        }
        else
        {
            this.LastSyncTime = $"{(int)timeSpan.TotalDays} days ago";
        }
    }

    private async Task SyncNowAsync()
    {
        this.IsSyncing = true;
        this.SyncStatusText = "Syncing...";
        this.SyncStatusColor = Colors.Orange;

        try
        {
            await this.googleSyncService.SyncAllGroupsAsync();
            
            this.SyncStatusText = "Synced";
            this.SyncStatusColor = Colors.Green;
            this.UpdateLastSyncTime();
            
            // Reset to "Connected" after 2 seconds
            await Task.Delay(2000);
            if (!this.IsSyncing) // Check if another sync hasn't started
            {
                this.SyncStatusText = "Connected";
            }
        }
        catch (Exception ex)
        {
            this.SyncStatusText = "Sync failed";
            this.SyncStatusColor = Colors.Red;
            
            // Show error in a user-friendly way
            await Application.Current.MainPage.DisplayAlert("Sync Error", ex.Message, "OK");
        }
        finally
        {
            this.IsSyncing = false;
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
