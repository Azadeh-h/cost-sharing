using CostSharingApp.ViewModels.Dashboard;


namespace CostSharingApp.Views.Dashboard;

/// <summary>
/// Transaction history page showing all user's expenses with filters.
/// </summary>
public partial class TransactionHistoryPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionHistoryPage"/> class.
    /// </summary>
    /// <param name="viewModel">The transaction history view model.</param>
    public TransactionHistoryPage(TransactionHistoryViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (this.BindingContext is TransactionHistoryViewModel viewModel)
        {
            viewModel.LoadTransactionsCommand.Execute(null);
        }
    }
}
