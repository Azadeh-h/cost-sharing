namespace CostSharingApp;

/// <summary>
/// Application shell providing navigation structure.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// </summary>
    public AppShell()
    {
        this.InitializeComponent();

        // Register routes for pages NOT in AppShell.xaml (to avoid duplicate registration)
        // Note: "dashboard" and "groups" are already defined in AppShell.xaml
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
        
        // Google integration routes
        Routing.RegisterRoute("conflictresolution", typeof(Views.ConflictResolutionPage));
    }
}
