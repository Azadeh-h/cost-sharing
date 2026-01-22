// <copyright file="AuthViewModel.cs" company="CostSharingApp">
// Copyright (c) CostSharingApp. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CostSharing.Core.Models;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels.Auth;

/// <summary>
/// ViewModel for the authentication page (Sign In / Sign Up).
/// </summary>
public partial class AuthViewModel : BaseViewModel
{
    private readonly IAuthService authService;
    private readonly ISessionService sessionService;
    private readonly ILoggingService loggingService;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private bool isSignInMode = true;

    [ObservableProperty]
    private string emailError = string.Empty;

    [ObservableProperty]
    private string passwordError = string.Empty;

    [ObservableProperty]
    private string confirmPasswordError = string.Empty;

    [ObservableProperty]
    private string displayNameError = string.Empty;

    [ObservableProperty]
    private string generalError = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthViewModel"/> class.
    /// </summary>
    /// <param name="authService">Authentication service.</param>
    /// <param name="sessionService">Session persistence service.</param>
    /// <param name="loggingService">Logging service.</param>
    public AuthViewModel(
        IAuthService authService,
        ISessionService sessionService,
        ILoggingService loggingService)
    {
        this.authService = authService;
        this.sessionService = sessionService;
        this.loggingService = loggingService;
        this.Title = "Welcome";
    }

    /// <summary>
    /// Gets the text for the toggle mode button.
    /// </summary>
    public string ToggleModeText => this.IsSignInMode
        ? "Don't have an account? Sign Up"
        : "Already have an account? Sign In";

    /// <summary>
    /// Gets the text for the primary action button.
    /// </summary>
    public string ActionButtonText => this.IsSignInMode ? "Sign In" : "Create Account";

    /// <summary>
    /// Validates the email format.
    /// </summary>
    /// <returns>True if valid.</returns>
    public bool ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(this.Email))
        {
            this.EmailError = "Email is required";
            return false;
        }

        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(this.Email, emailPattern))
        {
            this.EmailError = "Please enter a valid email address";
            return false;
        }

        this.EmailError = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates the password requirements.
    /// </summary>
    /// <returns>True if valid.</returns>
    public bool ValidatePassword()
    {
        if (string.IsNullOrWhiteSpace(this.Password))
        {
            this.PasswordError = "Password is required";
            return false;
        }

        if (this.Password.Length < 8)
        {
            this.PasswordError = "Password must be at least 8 characters";
            return false;
        }

        if (!this.Password.Any(char.IsDigit))
        {
            this.PasswordError = "Password must contain at least one number";
            return false;
        }

        this.PasswordError = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates password confirmation matches.
    /// </summary>
    /// <returns>True if valid.</returns>
    public bool ValidateConfirmPassword()
    {
        if (!this.IsSignInMode)
        {
            if (this.Password != this.ConfirmPassword)
            {
                this.ConfirmPasswordError = "Passwords do not match";
                return false;
            }
        }

        this.ConfirmPasswordError = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates display name for sign up.
    /// </summary>
    /// <returns>True if valid.</returns>
    public bool ValidateDisplayName()
    {
        if (!this.IsSignInMode)
        {
            if (string.IsNullOrWhiteSpace(this.DisplayName))
            {
                this.DisplayNameError = "Display name is required";
                return false;
            }

            if (this.DisplayName.Length > 100)
            {
                this.DisplayNameError = "Display name must be 100 characters or less";
                return false;
            }
        }

        this.DisplayNameError = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates all form fields.
    /// </summary>
    /// <returns>True if all valid.</returns>
    public bool ValidateAll()
    {
        var emailValid = this.ValidateEmail();
        var passwordValid = this.ValidatePassword();
        var confirmValid = this.ValidateConfirmPassword();
        var nameValid = this.ValidateDisplayName();

        return emailValid && passwordValid && confirmValid && nameValid;
    }

    /// <summary>
    /// Clears all error messages.
    /// </summary>
    private void ClearErrors()
    {
        this.EmailError = string.Empty;
        this.PasswordError = string.Empty;
        this.ConfirmPasswordError = string.Empty;
        this.DisplayNameError = string.Empty;
        this.GeneralError = string.Empty;
    }

    /// <summary>
    /// Clears the form fields.
    /// </summary>
    private void ClearForm()
    {
        this.Email = string.Empty;
        this.Password = string.Empty;
        this.ConfirmPassword = string.Empty;
        this.DisplayName = string.Empty;
        this.ClearErrors();
    }

    [RelayCommand]
    private void ToggleMode()
    {
        this.IsSignInMode = !this.IsSignInMode;
        this.ClearErrors();
        this.OnPropertyChanged(nameof(this.ToggleModeText));
        this.OnPropertyChanged(nameof(this.ActionButtonText));
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        this.ClearErrors();

        if (!this.ValidateAll())
        {
            return;
        }

        this.IsBusy = true;

        try
        {
            bool success;

            if (this.IsSignInMode)
            {
                success = await this.SignInAsync();
            }
            else
            {
                success = await this.SignUpAsync();
            }

            if (success)
            {
                // Navigate to dashboard
                await Shell.Current.GoToAsync("//dashboard");
            }
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Authentication error", ex);
            this.GeneralError = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    private async Task<bool> SignInAsync()
    {
        var result = await this.authService.LoginAsync(this.Email, this.Password);

        if (!result)
        {
            // Check if user exists to give better error message
            var users = await this.authService.GetAllUsersAsync();
            var userExists = users.Any(u => u.Email.Equals(this.Email, StringComparison.OrdinalIgnoreCase));

            if (!userExists)
            {
                this.GeneralError = "No account found with this email. Please sign up first.";
            }
            else
            {
                this.GeneralError = "Invalid email or password. Please try again.";
            }

            return false;
        }

        // Save session
        var currentUser = this.authService.GetCurrentUser();
        if (currentUser != null)
        {
            await this.sessionService.SaveSessionAsync(currentUser.Id);
        }

        this.loggingService.LogInfo($"User signed in: {this.Email}");
        return true;
    }

    private async Task<bool> SignUpAsync()
    {
        var result = await this.authService.RegisterAsync(
            this.Email,
            this.Password,
            this.DisplayName);

        if (!result)
        {
            this.GeneralError = "An account with this email already exists. Please sign in instead.";
            return false;
        }

        // Save session
        var currentUser = this.authService.GetCurrentUser();
        if (currentUser != null)
        {
            // Update account type to Email
            currentUser.AccountType = AccountType.Email;

            await this.sessionService.SaveSessionAsync(currentUser.Id);
        }

        this.loggingService.LogInfo($"User signed up: {this.Email}");
        return true;
    }
}
