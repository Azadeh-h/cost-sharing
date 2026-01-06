namespace CostSharingApp.ViewModels.Debts;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CostSharing.Core.Algorithms;
using CostSharing.Core.Models;
using CostSharing.Core.Services;
using CostSharingApp.Services;

/// <summary>
/// View model for simplified debts page showing optimized settlement plan.
/// </summary>
public partial class SimplifiedDebtsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IDebtCalculationService debtCalculationService;
    private readonly IGroupService groupService;
    private readonly IExpenseService expenseService;
    private readonly ISettlementService settlementService;
    private readonly IAuthService authService;
    private readonly DebtSimplificationAlgorithm simplificationAlgorithm;
    private Guid groupId;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int originalTransactionCount;

    [ObservableProperty]
    private int simplifiedTransactionCount;

    [ObservableProperty]
    private int transactionsSaved;

    [ObservableProperty]
    private decimal totalAmount;

    [ObservableProperty]
    private ObservableCollection<SimplifiedTransactionViewModel> simplifiedTransactions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplifiedDebtsViewModel"/> class.
    /// </summary>
    /// <param name="debtCalculationService">The debt calculation service.</param>
    /// <param name="groupService">The group service.</param>
    /// <param name="expenseService">The expense service.</param>
    /// <param name="settlementService">The settlement service.</param>
    /// <param name="authService">The authentication service.</param>
    public SimplifiedDebtsViewModel(
        IDebtCalculationService debtCalculationService,
        IGroupService groupService,
        IExpenseService expenseService,
        ISettlementService settlementService,
        IAuthService authService)
    {
        this.debtCalculationService = debtCalculationService;
        this.groupService = groupService;
        this.expenseService = expenseService;
        this.settlementService = settlementService;
        this.authService = authService;
        this.simplificationAlgorithm = new DebtSimplificationAlgorithm();
    }

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("groupId", out var groupIdObj) && groupIdObj is string groupIdStr)
        {
            this.groupId = Guid.Parse(groupIdStr);
            this.LoadSimplifiedDebtsAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads and calculates simplified debts for the group.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task LoadSimplifiedDebtsAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;

            // Load expenses for the group
            var expenses = await this.expenseService.GetGroupExpensesAsync(this.groupId);
            if (!expenses.Any())
            {
                this.SimplifiedTransactions.Clear();
                return;
            }

            // Load expense splits
            var allSplits = new List<ExpenseSplit>();
            foreach (var expense in expenses)
            {
                var splits = await this.expenseService.GetExpenseSplitsAsync(expense.Id);
                allSplits.AddRange(splits);
            }

            // Load settlements
            var settlements = await this.settlementService.GetGroupSettlementsAsync(this.groupId);

            // Calculate all debts accounting for settlements
            var allDebts = this.debtCalculationService.CalculateDebts(expenses, allSplits, settlements);
            this.OriginalTransactionCount = allDebts.Count;

            // Apply Min-Cash-Flow algorithm
            var simplifiedTx = this.simplificationAlgorithm.SimplifyDebts(allDebts);
            this.SimplifiedTransactionCount = simplifiedTx.Count;
            this.TransactionsSaved = this.OriginalTransactionCount - this.SimplifiedTransactionCount;
            this.TotalAmount = simplifiedTx.Sum(t => t.Amount);

            // Get group to access member names
            var group = await this.groupService.GetGroupAsync(this.groupId);
            if (group == null)
            {
                return;
            }

            var members = await this.groupService.GetGroupMembersAsync(this.groupId);

            // Convert to view models with user names
            this.SimplifiedTransactions.Clear();
            foreach (var tx in simplifiedTx)
            {
                var fromMember = members.FirstOrDefault(m => m.UserId == tx.FromUserId);
                var toMember = members.FirstOrDefault(m => m.UserId == tx.ToUserId);

                this.SimplifiedTransactions.Add(new SimplifiedTransactionViewModel
                {
                    FromUserId = tx.FromUserId,
                    FromUserName = fromMember?.UserId.ToString().Substring(0, 8) ?? "Unknown",
                    ToUserId = tx.ToUserId,
                    ToUserName = toMember?.UserId.ToString().Substring(0, 8) ?? "Unknown",
                    Amount = tx.Amount
                });
            }
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Records a settlement transaction.
    /// </summary>
    /// <param name="transaction">The transaction to record.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task RecordSettlementAsync(SimplifiedTransactionViewModel transaction)
    {
        if (transaction == null)
        {
            return;
        }

        var confirmed = await Application.Current!.MainPage!.DisplayAlert(
            "Record Settlement",
            $"{transaction.FromUserName} paid ${transaction.Amount:F2} to {transaction.ToUserName}?",
            "Yes, Record It",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        try
        {
            this.IsBusy = true;

            // Get current user
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    "User not authenticated",
                    "OK");
                return;
            }

            // Create settlement record
            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                GroupId = this.groupId,
                PaidBy = transaction.FromUserId,
                PaidTo = transaction.ToUserId,
                Amount = transaction.Amount,
                SettlementDate = DateTime.UtcNow,
                RecordedBy = currentUser.Id,
                Status = SettlementStatus.Pending
            };

            // Save settlement via SettlementService
            await this.settlementService.RecordSettlementAsync(settlement);

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                "Settlement recorded successfully!",
                "OK");

            // Refresh the simplified debts list
            await this.LoadSimplifiedDebtsAsync();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Failed to record settlement: {ex.Message}",
                "OK");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Navigates to the detailed debts view.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task ViewDetailedDebtsAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}

/// <summary>
/// View model for a simplified transaction item.
/// </summary>
public class SimplifiedTransactionViewModel
{
    /// <summary>
    /// Gets or sets the user ID who owes money.
    /// </summary>
    public Guid FromUserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who owes money.
    /// </summary>
    public string FromUserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID who is owed money.
    /// </summary>
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who is owed money.
    /// </summary>
    public string ToUserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount to be paid.
    /// </summary>
    public decimal Amount { get; set; }
}
