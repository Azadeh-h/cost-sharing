using System.ComponentModel;
using System.Runtime.CompilerServices;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels;

public class ConflictResolutionViewModel : INotifyPropertyChanged
{
    private readonly IGoogleSyncService googleSyncService;
    private string groupId;
    private string groupName;
    private DateTime localLastModified;
    private DateTime remoteLastModified;
    private string conflictDescription;

    public ConflictResolutionViewModel(IGoogleSyncService googleSyncService)
    {
        this.googleSyncService = googleSyncService;
        
        this.KeepLocalCommand = new Command(async () => await this.KeepLocalAsync());
        this.KeepRemoteCommand = new Command(async () => await this.KeepRemoteAsync());
        this.CancelCommand = new Command(async () => await this.CancelAsync());
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public string GroupName
    {
        get => this.groupName;
        set
        {
            if (this.groupName != value)
            {
                this.groupName = value;
                this.OnPropertyChanged();
            }
        }
    }

    public DateTime LocalLastModified
    {
        get => this.localLastModified;
        set
        {
            if (this.localLastModified != value)
            {
                this.localLastModified = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.LocalLastModifiedText));
            }
        }
    }

    public DateTime RemoteLastModified
    {
        get => this.remoteLastModified;
        set
        {
            if (this.remoteLastModified != value)
            {
                this.remoteLastModified = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.RemoteLastModifiedText));
            }
        }
    }

    public string ConflictDescription
    {
        get => this.conflictDescription;
        set
        {
            if (this.conflictDescription != value)
            {
                this.conflictDescription = value;
                this.OnPropertyChanged();
            }
        }
    }

    public string LocalLastModifiedText => this.LocalLastModified.ToLocalTime().ToString("g");
    public string RemoteLastModifiedText => this.RemoteLastModified.ToLocalTime().ToString("g");

    public Command KeepLocalCommand { get; }
    public Command KeepRemoteCommand { get; }
    public Command CancelCommand { get; }

    public void Initialize(string groupId, string groupName, DateTime localModified, DateTime remoteModified)
    {
        this.groupId = groupId;
        this.GroupName = groupName;
        this.LocalLastModified = localModified;
        this.RemoteLastModified = remoteModified;
        
        this.ConflictDescription = 
            "Both the local and remote versions of this group have been modified. " +
            "Please choose which version to keep. The other version will be overwritten.";
    }

    private async Task KeepLocalAsync()
    {
        try
        {
            // Resolve conflict by keeping local version
            await this.googleSyncService.ResolveConflictAsync(Guid.Parse(this.groupId), keepLocal: true);
            
            await Application.Current.MainPage.DisplayAlert(
                "Conflict Resolved", 
                "Local version has been kept and uploaded to Google Drive.", 
                "OK");
            
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task KeepRemoteAsync()
    {
        try
        {
            // Resolve conflict by keeping remote version
            await this.googleSyncService.ResolveConflictAsync(Guid.Parse(this.groupId), keepLocal: false);
            
            await Application.Current.MainPage.DisplayAlert(
                "Conflict Resolved", 
                "Remote version has been downloaded and applied locally.", 
                "OK");
            
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
