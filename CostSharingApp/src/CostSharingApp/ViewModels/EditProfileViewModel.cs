using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels;

/// <summary>
/// ViewModel for the Edit Profile page.
/// </summary>
public partial class EditProfileViewModel : BaseViewModel
{
    private readonly IAuthService authService;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string? phone;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string successMessage = string.Empty;

    [ObservableProperty]
    private bool hasSuccess;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditProfileViewModel"/> class.
    /// </summary>
    /// <param name="authService">Authentication service.</param>
    public EditProfileViewModel(IAuthService authService)
    {
        this.authService = authService;
        this.Title = "Edit Profile";
        this.LoadCurrentUserAsync();
    }

    /// <summary>
    /// Loads current user data.
    /// </summary>
    private void LoadCurrentUserAsync()
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser != null)
            {
                this.Name = currentUser.Name;
                this.Email = currentUser.Email;
                this.Phone = currentUser.Phone;
            }
        }
        catch
        {
            // Silent fail - user data will just be empty
        }
    }

    /// <summary>
    /// Saves the user profile.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        this.HasError = false;
        this.HasSuccess = false;

        // Validate
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            this.ErrorMessage = "Name is required";
            this.HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(this.Email))
        {
            this.ErrorMessage = "Email is required";
            this.HasError = true;
            return;
        }

        if (!IsValidEmail(this.Email))
        {
            this.ErrorMessage = "Please enter a valid email address";
            this.HasError = true;
            return;
        }

        try
        {
            this.IsBusy = true;

            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                this.ErrorMessage = "No user logged in";
                this.HasError = true;
                return;
            }

            var success = await this.authService.UpdateUserAsync(
                currentUser.Id,
                this.Name.Trim(),
                this.Email.Trim().ToLowerInvariant(),
                string.IsNullOrWhiteSpace(this.Phone) ? null : this.Phone.Trim());

            if (success)
            {
                this.SuccessMessage = "Profile updated successfully!";
                this.HasSuccess = true;

                // Go back after a brief delay
                await Task.Delay(1000);
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                this.ErrorMessage = "Email already in use by another user";
                this.HasError = true;
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Error: {ex.Message}";
            this.HasError = true;
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Cancels and goes back.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Validates email format.
    /// </summary>
    /// <param name="email">Email to validate.</param>
    /// <returns>True if valid.</returns>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }
}
