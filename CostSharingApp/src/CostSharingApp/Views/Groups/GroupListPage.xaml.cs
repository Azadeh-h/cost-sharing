namespace CostSharingApp.Views.Groups;

/// <summary>
/// Page displaying the list of user's groups.
/// </summary>
public partial class GroupListPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupListPage"/> class.
    /// </summary>
    public GroupListPage()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Called when the page appears.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load groups when page appears
        if (this.BindingContext is ViewModels.Groups.GroupListViewModel viewModel)
        {
            viewModel.LoadGroupsCommand.Execute(null);
        }
    }
}
