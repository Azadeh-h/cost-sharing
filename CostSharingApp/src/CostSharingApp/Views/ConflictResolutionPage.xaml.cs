using CostSharingApp.ViewModels;

namespace CostSharingApp.Views;

public partial class ConflictResolutionPage : ContentPage
{
    private readonly ConflictResolutionViewModel viewModel;

    public ConflictResolutionPage(ConflictResolutionViewModel viewModel)
    {
        this.InitializeComponent();
        this.viewModel = viewModel;
        this.BindingContext = viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        
        // Parameters will be passed from the navigation call
        // Example: await Shell.Current.GoToAsync("conflictresolution", new Dictionary<string, object> { ... });
    }
}
