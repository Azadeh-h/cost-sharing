using CostSharingApp.Services;

namespace CostSharingApp;

/// <summary>
/// Application shell providing navigation structure.
/// </summary>
public partial class AppShell : Shell
{
    private readonly BackgroundSyncService? syncService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// </summary>
    public AppShell()
    {
        this.InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("dashboard", typeof(Views.Dashboard.DashboardPage));
        Routing.RegisterRoute("transactionhistory", typeof(Views.Dashboard.TransactionHistoryPage));
        Routing.RegisterRoute("creategroup", typeof(Views.Groups.CreateGroupPage));
        Routing.RegisterRoute("groupdetails", typeof(Views.Groups.GroupDetailsPage));
        Routing.RegisterRoute("editgroup", typeof(Views.Groups.CreateGroupPage));
        Routing.RegisterRoute("invitemember", typeof(Views.Members.InviteMemberPage));
        Routing.RegisterRoute("acceptinvitation", typeof(Views.Members.AcceptInvitationPage));
        Routing.RegisterRoute("addexpense", typeof(Views.Expenses.AddExpensePage));
        Routing.RegisterRoute("expensedetails", typeof(Views.Expenses.ExpenseDetailsPage));
        Routing.RegisterRoute("customsplit", typeof(Views.Expenses.CustomSplitPage));
        Routing.RegisterRoute("simplifieddebts", typeof(Views.Debts.SimplifiedDebtsPage));

        // Get sync service from DI if available
        try
        {
            this.syncService = Handler?.MauiContext?.Services.GetService<BackgroundSyncService>();
            if (this.syncService != null)
            {
                this.syncService.SyncStatusChanged += this.OnSyncStatusChanged;
                this.UpdateSyncStatusUI(this.syncService.CurrentStatus);
            }
        }
        catch
        {
            // Service not registered yet, ignore
        }
    }

    private void OnSyncStatusChanged(object? sender, SyncStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => this.UpdateSyncStatusUI(e.Status));
    }

    private void UpdateSyncStatusUI(SyncStatus status)
    {
        var icon = this.FindByName<Label>("SyncStatusIcon");
        var label = this.FindByName<Label>("SyncStatusLabel");

        if (icon == null || label == null)
        {
            return;
        }

        switch (status)
        {
            case SyncStatus.Synced:
                icon.Text = "✓";
                label.Text = "Synced";
                break;
            case SyncStatus.Syncing:
                icon.Text = "🔄";
                label.Text = "Syncing...";
                break;
            case SyncStatus.Offline:
                icon.Text = "⚠️";
                label.Text = "Offline";
                break;
            case SyncStatus.Error:
                icon.Text = "❌";
                label.Text = "Sync Error";
                break;
        }
    }
}
