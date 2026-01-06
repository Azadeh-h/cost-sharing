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
        try
        {
            System.Diagnostics.Debug.WriteLine("App constructor starting...");
            _cacheService = cacheService;
            _authService = authService;
            this.InitializeComponent();
            System.Diagnostics.Debug.WriteLine("App constructor completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in App constructor: {ex}");
            throw;
        }
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
            System.Diagnostics.Debug.WriteLine("CreateWindow starting...");
            var window = new Window(new AppShell());
            System.Diagnostics.Debug.WriteLine("Window and AppShell created successfully");

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

            System.Diagnostics.Debug.WriteLine("CreateWindow completed successfully");
            return window;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FATAL ERROR in CreateWindow: {ex}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
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
                System.Diagnostics.Debug.WriteLine("Initializing cache service...");
                await Task.Run(async () => await _cacheService.InitializeAsync());
                System.Diagnostics.Debug.WriteLine("Cache service initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR initializing cache: {ex}");
                // Don't throw - allow app to continue even if cache init fails
            }
        }

        // Ensure user exists (no authentication needed, just a unique identifier)
        if (_authService != null && !_authService.IsAuthenticated())
        {
            System.Diagnostics.Debug.WriteLine("No user found, creating default user...");
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
                    await _authService.RegisterAsync("Device User", email, password);
                    System.Diagnostics.Debug.WriteLine($"User created with ID: {deviceId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"User loaded: {deviceId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user: {ex.Message}");
            }
        }
    }
}