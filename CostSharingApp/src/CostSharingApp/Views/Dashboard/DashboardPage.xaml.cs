using CostSharingApp.ViewModels.Dashboard;

namespace CostSharingApp.Views.Dashboard;

/// <summary>
/// Dashboard page showing total balance and per-group balances.
/// </summary>
public partial class DashboardPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardPage"/> class.
    /// </summary>
    /// <param name="viewModel">The dashboard view model.</param>
    public DashboardPage(DashboardViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (this.BindingContext is DashboardViewModel viewModel)
        {
            viewModel.LoadDashboardCommand.Execute(null);
        }
    }
}
