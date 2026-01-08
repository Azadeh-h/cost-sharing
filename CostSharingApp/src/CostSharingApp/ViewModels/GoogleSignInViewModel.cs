using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels;

public class GoogleSignInViewModel : INotifyPropertyChanged
{
    private readonly IGoogleAuthService googleAuthService;
    private readonly IGoogleSyncService googleSyncService;
    private bool isSignedIn;
    private bool isBusy;
    private string userEmail;
    private string statusMessage;

    public GoogleSignInViewModel(IGoogleAuthService googleAuthService, IGoogleSyncService googleSyncService)
    {
        this.googleAuthService = googleAuthService;
        this.googleSyncService = googleSyncService;
        
        this.SignInCommand = new Command(async () => await this.SignInAsync(), () => !this.IsBusy);
        this.SignOutCommand = new Command(async () => await this.SignOutAsync(), () => !this.IsBusy && this.IsSignedIn);
        this.EnableAutoSyncCommand = new Command(async () => await this.EnableAutoSyncAsync(), () => this.IsSignedIn && !this.IsBusy);
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
                ((Command)this.SignInCommand).ChangeCanExecute();
                ((Command)this.SignOutCommand).ChangeCanExecute();
                ((Command)this.EnableAutoSyncCommand).ChangeCanExecute();
            }
        }
    }

    public bool IsBusy
    {
        get => this.isBusy;
        set
        {
            if (this.isBusy != value)
            {
                this.isBusy = value;
                this.OnPropertyChanged();
                ((Command)this.SignInCommand).ChangeCanExecute();
                ((Command)this.SignOutCommand).ChangeCanExecute();
                ((Command)this.EnableAutoSyncCommand).ChangeCanExecute();
            }
        }
    }

    public string UserEmail
    {
        get => this.userEmail;
        set
        {
            if (this.userEmail != value)
            {
                this.userEmail = value;
                this.OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => this.statusMessage;
        set
        {
            if (this.statusMessage != value)
            {
                this.statusMessage = value;
                this.OnPropertyChanged();
            }
        }
    }

    public ICommand SignInCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand EnableAutoSyncCommand { get; }

    public async Task InitializeAsync()
    {
        await this.googleAuthService.InitializeAsync();
        this.IsSignedIn = this.googleAuthService.IsAuthenticated;
        
        if (this.IsSignedIn)
        {
            this.UserEmail = this.googleAuthService.GetCurrentUserEmail();
            this.StatusMessage = "Connected to Google";
        }
        else
        {
            this.StatusMessage = "Not connected";
        }
    }

    private async Task SignInAsync()
    {
        this.IsBusy = true;
        this.StatusMessage = "Signing in...";

        try
        {
            var success = await this.googleAuthService.AuthenticateAsync();
            
            if (success)
            {
                this.IsSignedIn = true;
                this.UserEmail = this.googleAuthService.GetCurrentUserEmail();
                this.StatusMessage = "Successfully signed in to Google";
            }
            else
            {
                this.StatusMessage = "Failed to sign in";
            }
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    private async Task SignOutAsync()
    {
        this.IsBusy = true;
        this.StatusMessage = "Signing out...";

        try
        {
            await this.googleAuthService.SignOutAsync();
            this.IsSignedIn = false;
            this.UserEmail = null;
            this.StatusMessage = "Signed out";
            
            // Stop auto-sync
            this.googleSyncService.StopAutoSync();
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    private async Task EnableAutoSyncAsync()
    {
        this.IsBusy = true;
        this.StatusMessage = "Enabling auto-sync...";

        try
        {
            // Start auto-sync with 30 second interval
            this.googleSyncService.StartAutoSync(30);
            
            // Perform initial sync
            await this.googleSyncService.SyncAllGroupsAsync();
            
            this.StatusMessage = "Auto-sync enabled";
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
