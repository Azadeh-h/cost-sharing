namespace CostSharingApp;

/// <summary>
/// Main application entry point.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Creates the main window for the application.
    /// </summary>
    /// <param name="activationState">Activation state including launch arguments.</param>
    /// <returns>The main window.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
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
}