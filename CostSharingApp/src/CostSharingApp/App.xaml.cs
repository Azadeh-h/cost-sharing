using CostSharingApp.Services;

namespace CostSharingApp;

/// <summary>
/// Main application entry point.
/// </summary>
public partial class App : Application
{
    private readonly ICacheService? _cacheService;
    private readonly IAuthService? _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service for initialization.</param>
    /// <param name="authService">Auth service for auto-login.</param>
    public App(ICacheService? cacheService = null, IAuthService? authService = null)
    {
        _cacheService = cacheService;
        _authService = authService;
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

            // Handle deep link if present
            if (activationState is not null)
            {
                var uri = activationState.State.TryGetValue("uri", out var uriString)
                    ? new Uri(uriString?.ToString() ?? string.Empty)
                    : null;

                if (uri is not null && uri.Scheme == "costsharingapp" && uri.Host == "invite")
                {
                    // Extract token from URI path (format: costsharingapp://invite/{token})
                    var token = uri.AbsolutePath.TrimStart('/');
                    if (!string.IsNullOrEmpty(token))
                    {
                        // Navigate to AcceptInvitationPage with token
                        Shell.Current.Dispatcher.Dispatch(async () =>
                        {
                            await Shell.Current.GoToAsync($"acceptinvitation?token={Uri.EscapeDataString(token)}");
                        });
                    }
                }
            }

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
            }
            catch (Exception)
            {
                // Don't throw - allow app to continue even if cache init fails
            }
        }

        // Ensure user exists (no authentication needed, just a unique identifier)
        if (_authService != null && !_authService.IsAuthenticated())
        {
            try
            {
                // Use device ID as unique identifier, or generate a GUID
                var deviceId = Preferences.Get("DeviceUserId", Guid.NewGuid().ToString());
                Preferences.Set("DeviceUserId", deviceId);
                
                var email = $"{deviceId}@device.local";
                var password = "default123"; // Simple password, not used for security
                
                // Try to login first, if fails, register
                var loginResult = await _authService.LoginAsync(email, password);
                if (!loginResult)
                {
                    await _authService.RegisterAsync(email, password, "Device User");
                }
            }
            catch (Exception)
            {
                // Silent fail - user creation is not critical
            }
        }
    }
}