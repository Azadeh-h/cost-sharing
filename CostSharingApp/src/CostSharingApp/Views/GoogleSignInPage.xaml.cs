using CostSharingApp.ViewModels;

namespace CostSharingApp.Views;

public partial class GoogleSignInPage : ContentPage
{
    private readonly GoogleSignInViewModel viewModel;

    public GoogleSignInPage(GoogleSignInViewModel viewModel)
    {
        this.InitializeComponent();
        this.viewModel = viewModel;
        this.BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await this.viewModel.InitializeAsync();
    }
}
