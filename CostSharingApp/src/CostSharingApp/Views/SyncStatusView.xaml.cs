using CostSharingApp.ViewModels;

namespace CostSharingApp.Views;

public partial class SyncStatusView : ContentView
{
    private readonly SyncStatusViewModel viewModel;

    public SyncStatusView(SyncStatusViewModel viewModel)
    {
        this.InitializeComponent();
        this.viewModel = viewModel;
        this.BindingContext = viewModel;
    }

    public async Task InitializeAsync()
    {
        await this.viewModel.InitializeAsync();
    }

    public void UpdateLastSyncTime()
    {
        this.viewModel.UpdateLastSyncTime();
    }
}
