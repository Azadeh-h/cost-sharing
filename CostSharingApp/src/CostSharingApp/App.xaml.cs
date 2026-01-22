using CostSharingApp.Services;
using CostSharing.Core.Services;

namespace CostSharingApp;

/// <summary>
/// Main application entry point.
/// </summary>
public partial class App : Application
{
    private readonly ICacheService? _cacheService;
    private readonly IAuthService? _authService;
    private readonly ILoggingService? _loggingService;
    private readonly ISessionService? _sessionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service for initialization.</param>
    /// <param name="authService">Auth service for auto-login.</param>
    /// <param name="loggingService">Logging service.</param>
    /// <param name="sessionService">Session service for persistence.</param>
    public App(
        ICacheService? cacheService = null, 
        IAuthService? authService = null,
        ILoggingService? loggingService = null,
        ISessionService? sessionService = null)
    {
        _cacheService = cacheService;
        _authService = authService;
        _loggingService = loggingService;
        _sessionService = sessionService;
        this.InitializeComponent();
    }

    /// <summary>
    /// Creates the main window for the application.
    /// </summary>
    /// <param name="activationState">Activation state including launch arguments.</param>
    /// <returns>The main window.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        try
        {
            var window = new Window(new AppShell());
            return window;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Called when the application starts.
    /// </summary>
    protected override async void OnStart()
    {
        base.OnStart();

        // Initialize cache asynchronously without blocking - only if service is available
        if (_cacheService != null)
        {
            try
            {
                await Task.Run(async () => await _cacheService.InitializeAsync());
                
                // Run database migrations
                if (_loggingService != null)
                {
                    await DatabaseMigrations.UpdateSaraChenEmailAsync(_cacheService, _loggingService);
                }
            }
            catch (Exception)
            {
                // Don't throw - allow app to continue even if cache init fails
            }
        }

        // Check for existing session and restore user
        if (_sessionService != null && _authService != null)
        {
            try
            {
                var userId = await _sessionService.GetSessionAsync();
                if (userId.HasValue)
                {
                    // Restore user from session
                    var restored = await _authService.RestoreSessionAsync(userId.Value);
                    if (!restored)
                    {
                        // User not found, clear invalid session
                        await _sessionService.ClearSessionAsync();
                        await NavigateToAuthAsync();
                    }
                    // If restored, stay on dashboard (default shell route)
                }
                else
                {
                    // No session, navigate to auth page
                    await NavigateToAuthAsync();
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogError("Failed to restore session", ex);
                await NavigateToAuthAsync();
            }
        }
    }

    /// <summary>
    /// Navigates to the authentication page.
    /// </summary>
    private async Task NavigateToAuthAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // Small delay to ensure shell is ready
            await Task.Delay(100);
            try
            {
                await Shell.Current.GoToAsync("//auth");
            }
            catch
            {
                // Shell might not be ready, try again later
            }
        });
    }
}