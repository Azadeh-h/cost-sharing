using CostSharingApp.ViewModels.Groups;

namespace CostSharingApp.Views.Groups;

/// <summary>
/// Page displaying group details and members.
/// </summary>
public partial class GroupDetailsPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupDetailsPage"/> class.
    /// </summary>
    public GroupDetailsPage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Refresh data when returning to page (after adding expense or recording settlement)
        if (this.BindingContext is GroupDetailsViewModel viewModel)
        {
            viewModel.RefreshCommand?.Execute(null);
        }
    }
}
