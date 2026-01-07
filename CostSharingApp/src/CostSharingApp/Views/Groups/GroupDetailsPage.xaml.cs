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
    /// <param name="viewModel">The group details view model.</param>
    public GroupDetailsPage(ViewModels.Groups.GroupDetailsViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Force refresh data when page appears to ensure we have the latest group
        if (this.BindingContext is GroupDetailsViewModel viewModel && viewModel.RefreshCommand != null)
        {
            // Don't use the cached group - force reload from query parameters
            viewModel.RefreshCommand.Execute(null);
        }
    }
}
