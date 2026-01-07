namespace CostSharingApp.Views.Groups;

/// <summary>
/// Page for creating a new group.
/// </summary>
public partial class CreateGroupPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateGroupPage"/> class.
    /// </summary>
    /// <param name="viewModel">The create group view model.</param>
    public CreateGroupPage(ViewModels.Groups.CreateGroupViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}
