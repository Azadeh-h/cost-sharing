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
        this.InitializeComponent();
        this.BindingContext = viewModel;
        _viewModel = viewModel;
        _authService = authService;
    }

    /// <inheritdoc/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Ensure user exists (no authentication needed, just a unique identifier)
        if (!_authService.IsAuthenticated())
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

        if (this.BindingContext is DashboardViewModel viewModel)
        {
            viewModel.LoadDashboardCommand.Execute(null);
        }
    }
}
