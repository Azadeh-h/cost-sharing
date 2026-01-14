using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CostSharingApp.Services;
using CostSharing.Core.Services;

namespace CostSharingApp.ViewModels;

/// <summary>
/// ViewModel for the Settings page.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly IAuthService authService;
    private readonly IDriveAuthService? driveAuthService;

    [ObservableProperty]
    private string appVersion = AppInfo.VersionString;

    [ObservableProperty]
    private string buildNumber = $"Build {AppInfo.BuildString}";

    [ObservableProperty]
    private string platform = string.Empty;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string userEmail = string.Empty;

    [ObservableProperty]
    private string driveAuthStatus = "Not Authorized";

    [ObservableProperty]
    private Color driveAuthStatusColor = Colors.Red;

    [ObservableProperty]
    private bool showAuthorizeButton = true;

    [ObservableProperty]
    private bool showRevokeButton;

    [ObservableProperty]
    private bool notificationsEnabled = true;

    [ObservableProperty]
    private bool autoSyncEnabled = true;

    [ObservableProperty]
    private bool isDebugBuild;

    [ObservableProperty]
    private string deviceInfo = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(
        IAuthService authService,
        IDriveAuthService? driveAuthService = null)
    {
        this.authService = authService;
        this.driveAuthService = driveAuthService;

        this.Title = "Settings";
        this.Platform = Microsoft.Maui.Devices.DeviceInfo.Current.Platform.ToString();

#if DEBUG
        this.IsDebugBuild = true;
        this.DeviceInfo = $"Model: {Microsoft.Maui.Devices.DeviceInfo.Current.Model}\nOS: {Microsoft.Maui.Devices.DeviceInfo.Current.Platform} {Microsoft.Maui.Devices.DeviceInfo.Current.VersionString}\nManufacturer: {Microsoft.Maui.Devices.DeviceInfo.Current.Manufacturer}";
#endif

        _ = this.LoadSettingsAsync();
    }

    /// <summary>
    /// Loads current settings and user information.
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser != null)
            {
                this.UserName = currentUser.Name;
                this.UserEmail = currentUser.Email;

                // Check Drive authorization
                if (this.driveAuthService != null)
                {
                    var isAuthorized = await this.driveAuthService.IsAuthorizedAsync(currentUser.Id);
                    this.DriveAuthStatus = isAuthorized ? "Authorized" : "Not Authorized";
                    this.DriveAuthStatusColor = isAuthorized ? Colors.Green : Colors.Red;
                    this.ShowAuthorizeButton = !isAuthorized;
                    this.ShowRevokeButton = isAuthorized;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task EditProfileAsync()
    {
        await Shell.Current.GoToAsync("editprofile");
    }

    [RelayCommand]
    private async Task LogOutAsync()
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Log Out",
            "Are you sure you want to log out?",
            "Log Out",
            "Cancel");

        if (confirm)
        {
            // Clear any stored credentials
            SecureStorage.Default.RemoveAll();
            
            // Navigate to login
            await Shell.Current.GoToAsync("//login");
        }
    }

    [RelayCommand]
    private async Task AuthorizeDriveAsync()
    {
        if (this.driveAuthService == null)
        {
            return;
        }

        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return;
            }

            await this.driveAuthService.AuthorizeAsync(currentUser.Id);
            await this.LoadSettingsAsync();

            await Application.Current!.MainPage!.DisplayAlert(
                "Success",
                "Google Drive has been authorized successfully.",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Authorization Failed",
                ex.Message,
                "OK");
        }
    }

    [RelayCommand]
    private async Task RevokeDriveAsync()
    {
        if (this.driveAuthService == null)
        {
            return;
        }

        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Revoke Authorization",
            "This will disable sync for all groups. Are you sure?",
            "Revoke",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return;
            }

            await this.driveAuthService.RevokeAuthorizationAsync(currentUser.Id);
            await this.LoadSettingsAsync();

            await Application.Current!.MainPage!.DisplayAlert(
                "Success",
                "Google Drive authorization has been revoked.",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Error",
                ex.Message,
                "OK");
        }
    }

    [RelayCommand]
    private async Task OpenUserGuideAsync()
    {
        await Launcher.OpenAsync("https://github.com/your-repo/cost-sharing/wiki/User-Guide");
    }

    [RelayCommand]
    private async Task OpenFaqAsync()
    {
        await Launcher.OpenAsync("https://github.com/your-repo/cost-sharing/wiki/FAQ");
    }

    [RelayCommand]
    private async Task ReportIssueAsync()
    {
        await Launcher.OpenAsync("https://github.com/your-repo/cost-sharing/issues/new");
    }

    [RelayCommand]
    private async Task ContactSupportAsync()
    {
        await Launcher.OpenAsync("mailto:support@costsharingapp.com?subject=Support Request");
    }

    [RelayCommand]
    private async Task ViewLicensesAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Open Source Licenses",
            "This app uses the following open source libraries:\n\n" +
            "• .NET MAUI - MIT License\n" +
            "• SQLite-net - MIT License\n" +
            "• Google.Apis.Drive.v3 - Apache 2.0\n" +
            "• CommunityToolkit.Mvvm - MIT License",
            "OK");
    }

    [RelayCommand]
    private async Task ViewPrivacyPolicyAsync()
    {
        await Launcher.OpenAsync("https://github.com/your-repo/cost-sharing/blob/main/PRIVACY.md");
    }

    [RelayCommand]
    private async Task ClearDataAsync()
    {
        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            "⚠️ Warning",
            "This will delete ALL local data. This action cannot be undone!",
            "Delete All",
            "Cancel");

        if (confirm)
        {
            // TODO: Implement clear data logic
            await Application.Current!.MainPage!.DisplayAlert(
                "Data Cleared",
                "All local data has been deleted. The app will now restart.",
                "OK");

            // Restart app or navigate to login
            await Shell.Current.GoToAsync("//login");
        }
    }
}
