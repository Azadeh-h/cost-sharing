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
        System.Diagnostics.Debug.WriteLine("=== CreateGroupPage constructor called ===");
        this.InitializeComponent();
        this.BindingContext = viewModel;
        System.Diagnostics.Debug.WriteLine("=== CreateGroupPage initialized ===");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("=== CreateGroupPage OnAppearing ===");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        System.Diagnostics.Debug.WriteLine("=== CreateGroupPage OnDisappearing ===");
    }
}
