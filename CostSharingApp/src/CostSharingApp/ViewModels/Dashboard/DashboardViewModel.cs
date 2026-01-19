using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CostSharing.Core.Models;
using CostSharing.Core.Services;
using CostSharingApp.Services;


namespace CostSharingApp.ViewModels.Dashboard;

/// <summary>
/// View model for the dashboard page.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IGroupService groupService;
    private readonly IExpenseService expenseService;
    private readonly IDebtCalculationService debtCalculationService;
    private readonly ISettlementService settlementService;
    private readonly IAuthService authService;
    private readonly IErrorService errorService;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private decimal totalBalance;

    [ObservableProperty]
    private decimal totalOwed;

    [ObservableProperty]
    private decimal totalOwing;

    [ObservableProperty]
    private ObservableCollection<GroupBalanceViewModel> groupBalances = new();

    /// <summary>
    /// Gets the display total balance (always positive).
    /// </summary>
    public decimal DisplayTotalBalance => Math.Abs(TotalBalance);

    /// <summary>
    /// Gets the total balance color based on owed/owing status.
    /// </summary>
    public Color TotalBalanceColor
    {
        get
        {
            if (TotalBalance > 0)
            {
                return Colors.Green;
            }
            else if (TotalBalance < 0)
            {
                return Colors.Red;
            }
            else
            {
                return Colors.White;
            }
        }
    }

    /// <summary>
    /// Gets the total balance description.
    /// </summary>
    public string TotalBalanceDescription
    {
        get
        {
            if (TotalBalance > 0)
            {
                return "You're owed overall";
            }
            else if (TotalBalance < 0)
            {
                return "You owe overall";
            }
            else
            {
                return "All settled up";
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardViewModel"/> class.
    /// </summary>
    public DashboardViewModel(
        IGroupService groupService,
        IExpenseService expenseService,
        IDebtCalculationService debtCalculationService,
        ISettlementService settlementService,
        IAuthService authService,
        IErrorService errorService)
    {
        this.groupService = groupService;
        this.expenseService = expenseService;
        this.debtCalculationService = debtCalculationService;
        this.settlementService = settlementService;
        this.authService = authService;
        this.errorService = errorService;
    }

    /// <summary>
    /// Loads dashboard data including balances for all groups.
    /// </summary>
    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            this.IsRefreshing = true;

            var currentUser = this.authService.GetCurrentUser();
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Current user: {currentUser?.Id}, Name: {currentUser?.Name}");
            
            if (currentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("[Dashboard] No current user, returning");
                return;
            }

            // Load all groups for the user
            var groups = await this.groupService.GetUserGroupsAsync();
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Loaded {groups.Count} groups");

            this.GroupBalances.Clear();
            decimal totalOwed = 0;
            decimal totalOwing = 0;

            // Calculate balance for each group
            foreach (var group in groups)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Processing group: {group.Name} (ID: {group.Id})");
                
                var balance = await this.CalculateGroupBalanceAsync(group.Id, currentUser.Id);
                var members = await this.groupService.GetGroupMembersAsync(group.Id);
                
                // Add defensive check for group name
                var groupName = string.IsNullOrWhiteSpace(group.Name) ? "[Unnamed Group]" : group.Name;
                
                var groupBalance = new GroupBalanceViewModel
                {
                    GroupId = group.Id,
                    GroupName = groupName,
                    MemberCount = members?.Count ?? 0,
                    Balance = balance
                };

                // Ensure UI update happens on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    this.GroupBalances.Add(groupBalance);
                });

                if (balance > 0)
                {
                    totalOwed += balance;
                }
                else if (balance < 0)
                {
                    totalOwing += Math.Abs(balance);
                }
            }

            this.TotalOwed = totalOwed;
            this.TotalOwing = totalOwing;
            this.TotalBalance = totalOwed - totalOwing;
            
            // Notify computed properties
            this.OnPropertyChanged(nameof(DisplayTotalBalance));
            this.OnPropertyChanged(nameof(TotalBalanceColor));
            this.OnPropertyChanged(nameof(TotalBalanceDescription));
            
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Final totals - Owed: {totalOwed}, Owing: {totalOwing}, Balance: {this.TotalBalance}");
            System.Diagnostics.Debug.WriteLine($"[Dashboard] GroupBalances count: {this.GroupBalances.Count}");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Error",
                this.errorService.HandleException(ex, "loading dashboard"),
                "OK");
        }
        finally
        {
            this.IsBusy = false;
            this.IsRefreshing = false;
        }
    }

    /// <summary>
    /// Calculates the balance for a specific group and user.
    /// Positive = user is owed money, Negative = user owes money.
    /// </summary>
    private async Task<decimal> CalculateGroupBalanceAsync(Guid groupId, Guid userId)
    {
        System.Diagnostics.Debug.WriteLine($"[Dashboard] CalculateGroupBalanceAsync - GroupId: {groupId}, UserId: {userId}");
        
        // Get all expenses for the group
        var expenses = await this.expenseService.GetGroupExpensesAsync(groupId);
        
        if (!expenses.Any())
        {
            return 0;
        }

        // Get all expense splits
        var allSplits = new List<ExpenseSplit>();
        foreach (var expense in expenses)
        {
            var splits = await this.expenseService.GetExpenseSplitsAsync(expense.Id);
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Expense '{expense.Description}': {splits.Count} splits, PaidBy: {expense.PaidBy}");
            allSplits.AddRange(splits);
        }

        System.Diagnostics.Debug.WriteLine($"[Dashboard] Total splits: {allSplits.Count}");

        // Get settlements
        var settlements = await this.settlementService.GetGroupSettlementsAsync(groupId);
        System.Diagnostics.Debug.WriteLine($"[Dashboard] Found {settlements.Count} settlements");

        // Calculate all debts accounting for settlements
        var debts = this.debtCalculationService.CalculateDebts(expenses, allSplits, settlements);
        System.Diagnostics.Debug.WriteLine($"[Dashboard] Calculated {debts.Count} debts");

        // Check if this is a single-member group where user paid everything
        var members = await this.groupService.GetGroupMembersAsync(groupId);
        if (members.Count == 1 && expenses.All(e => e.PaidBy == userId))
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Single-member group - user paid all expenses, balance is 0");
        }

        // Calculate net balance for this user
        decimal balance = 0;

        foreach (var debt in debts)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Debt: {debt.DebtorId} owes {debt.CreditorId} ${debt.Amount}");
            
            if (debt.CreditorId == userId)
            {
                // User is owed money
                balance += debt.Amount;
                System.Diagnostics.Debug.WriteLine($"[Dashboard] User is OWED ${debt.Amount}, balance now: ${balance}");
            }
            else if (debt.DebtorId == userId)
            {
                // User owes money
                balance -= debt.Amount;
                System.Diagnostics.Debug.WriteLine($"[Dashboard] User OWES ${debt.Amount}, balance now: ${balance}");
            }
        }

        System.Diagnostics.Debug.WriteLine($"[Dashboard] Final balance for user {userId}: ${balance}");
        return balance;
    }

    /// <summary>
    /// Navigates to the group details page.
    /// </summary>
    [RelayCommand]
    private async Task ViewGroupAsync(GroupBalanceViewModel groupBalance)
    {
        if (groupBalance != null)
        {
            await Shell.Current.GoToAsync($"groupdetails?groupId={groupBalance.GroupId}");
        }
    }

    /// <summary>
    /// Navigates to transaction history page.
    /// </summary>
    [RelayCommand]
    private async Task ViewTransactionHistoryAsync()
    {
        await Shell.Current.GoToAsync("transactionhistory");
    }

    /// <summary>
    /// Navigates to create group page.
    /// </summary>
    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        await Shell.Current.GoToAsync("creategroup");
    }
}

/// <summary>
/// View model for a group balance item.
/// </summary>
public class GroupBalanceViewModel
{
    /// <summary>
    /// Gets or sets the group ID.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the member count.
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Gets or sets the balance. Positive = owed, Negative = owing.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Gets the display balance (always positive).
    /// </summary>
    public decimal DisplayBalance => Math.Abs(Balance);

    /// <summary>
    /// Gets the balance color based on owed/owing status.
    /// </summary>
    public Color BalanceColor
    {
        get
        {
            if (Balance > 0)
            {
                return Colors.Green;
            }
            else if (Balance < 0)
            {
                return Colors.Red;
            }
            else
            {
                return Colors.Gray;
            }
        }
    }

    /// <summary>
    /// Gets the balance status text.
    /// </summary>
    public string BalanceStatus
    {
        get
        {
            if (this.MemberCount == 1)
            {
                return "add members to split";
            }
            else if (Balance > 0)
            {
                return "you're owed";
            }
            else if (Balance < 0)
            {
                return "you owe";
            }
            else
            {
                return "settled up";
            }
        }
    }
}
