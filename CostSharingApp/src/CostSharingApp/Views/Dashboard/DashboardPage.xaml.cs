using CostSharingApp.ViewModels.Dashboard;

namespace CostSharingApp.Views.Dashboard;

/// <summary>
/// Dashboard page showing total balance and per-group balances.
/// </summary>
public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private readonly Services.IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardPage"/> class.
    /// </summary>
    /// <param name="viewModel">The dashboard view model.</param>
    /// <param name="authService">The authentication service.</param>
    public DashboardPage(DashboardViewModel viewModel, Services.IAuthService authService)
    {
        System.Diagnostics.Debug.WriteLine("=== DashboardPage constructor called ===");
        this.InitializeComponent();
        this.BindingContext = viewModel;
        _viewModel = viewModel;
        _authService = authService;
        System.Diagnostics.Debug.WriteLine($"=== BindingContext set to: {viewModel?.GetType().Name} ===");
    }

    /// <inheritdoc/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Ensure user exists (no authentication needed, just a unique identifier)
        if (!_authService.IsAuthenticated())
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

        if (this.BindingContext is DashboardViewModel viewModel)
        {
            viewModel.LoadDashboardCommand.Execute(null);
        }
    }
}
