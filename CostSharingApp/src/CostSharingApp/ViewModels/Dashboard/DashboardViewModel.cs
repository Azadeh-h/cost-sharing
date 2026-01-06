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
    private decimal totalBalance;

    [ObservableProperty]
    private decimal totalOwed;

    [ObservableProperty]
    private decimal totalOwing;

    [ObservableProperty]
    private ObservableCollection<GroupBalanceViewModel> groupBalances = new();

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

            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return;
            }

            // Load all groups for the user
            var groups = await this.groupService.GetUserGroupsAsync();

            this.GroupBalances.Clear();
            decimal totalOwed = 0;
            decimal totalOwing = 0;

            // Calculate balance for each group
            foreach (var group in groups)
            {
                var balance = await this.CalculateGroupBalanceAsync(group.Id, currentUser.Id);
                var members = await this.groupService.GetGroupMembersAsync(group.Id);
                
                var groupBalance = new GroupBalanceViewModel
                {
                    GroupId = group.Id,
                    GroupName = group.Name,
                    MemberCount = members?.Count ?? 0,
                    Balance = balance
                };

                this.GroupBalances.Add(groupBalance);

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
        }
    }

    /// <summary>
    /// Calculates the balance for a specific group and user.
    /// Positive = user is owed money, Negative = user owes money.
    /// </summary>
    private async Task<decimal> CalculateGroupBalanceAsync(Guid groupId, Guid userId)
    {
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
            allSplits.AddRange(splits);
        }

        // Get settlements
        var settlements = await this.settlementService.GetGroupSettlementsAsync(groupId);

        // Calculate all debts accounting for settlements
        var debts = this.debtCalculationService.CalculateDebts(expenses, allSplits, settlements);

        // Calculate net balance for this user
        decimal balance = 0;

        foreach (var debt in debts)
        {
            if (debt.CreditorId == userId)
            {
                // User is owed money
                balance += debt.Amount;
            }
            else if (debt.DebtorId == userId)
            {
                // User owes money
                balance -= debt.Amount;
            }
        }

        return balance;
    }

    /// <summary>
    /// Navigates to the group details page.
    /// </summary>
    [RelayCommand]
    private async Task ViewGroupAsync(Guid groupId)
    {
        await Shell.Current.GoToAsync($"groupdetails?groupId={groupId}");
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
    /// Gets the balance status text.
    /// </summary>
    public string BalanceStatus
    {
        get
        {
            if (Balance > 0)
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
