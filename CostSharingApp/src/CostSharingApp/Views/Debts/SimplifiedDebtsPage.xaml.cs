namespace CostSharingApp.Views.Debts;

/// <summary>
/// Page displaying simplified debt settlements using Min-Cash-Flow algorithm.
/// </summary>
public partial class SimplifiedDebtsPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimplifiedDebtsPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public SimplifiedDebtsPage(ViewModels.Debts.SimplifiedDebtsViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}
