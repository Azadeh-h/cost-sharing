namespace CostSharingApp.ViewModels.Dashboard;

using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CostSharingApp.Services;

/// <summary>
/// View model for the transaction history page.
/// </summary>
public partial class TransactionHistoryViewModel : ObservableObject
{
    private readonly IExpenseService expenseService;
    private readonly IGroupService groupService;
    private readonly IAuthService authService;
    private readonly IErrorService errorService;
    private List<TransactionViewModel> allTransactions = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private DateTime startDate;

    [ObservableProperty]
    private DateTime endDate;

    [ObservableProperty]
    private string selectedFilter = "All";

    [ObservableProperty]
    private Color filterAllColor;

    [ObservableProperty]
    private Color filterPaidColor;

    [ObservableProperty]
    private Color filterOweColor;

    [ObservableProperty]
    private decimal totalPaid;

    [ObservableProperty]
    private decimal totalOwed;

    [ObservableProperty]
    private decimal netBalance;

    /// <summary>
    /// Gets the filtered transactions collection.
    /// </summary>
    public ObservableCollection<TransactionViewModel> FilteredTransactions { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionHistoryViewModel"/> class.
    /// </summary>
    /// <param name="expenseService">The expense service.</param>
    /// <param name="groupService">The group service.</param>
    /// <param name="authService">The authentication service.</param>
    /// <param name="errorService">The error service.</param>
    public TransactionHistoryViewModel(
        IExpenseService expenseService,
        IGroupService groupService,
        IAuthService authService,
        IErrorService errorService)
    {
        this.expenseService = expenseService;
        this.groupService = groupService;
        this.authService = authService;
        this.errorService = errorService;

        // Initialize date range to last 30 days
        this.endDate = DateTime.Today;
        this.startDate = DateTime.Today.AddDays(-30);

        // Initialize filter colors
        this.UpdateFilterColors();
    }

    /// <summary>
    /// Gets the command to load transactions.
    /// </summary>
    public ICommand LoadTransactionsCommand => new AsyncRelayCommand(this.LoadTransactionsAsync);

    /// <summary>
    /// Gets the command to apply filters.
    /// </summary>
    public ICommand ApplyFiltersCommand => new AsyncRelayCommand(this.ApplyFiltersAsync);

    /// <summary>
    /// Gets the command to filter by type.
    /// </summary>
    public ICommand FilterByTypeCommand => new RelayCommand<string>(this.FilterByType);

    private async Task LoadTransactionsAsync()
    {
        try
        {
            this.IsBusy = true;
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                this.errorService.HandleException(new Exception("User not logged in"), "Please log in to view transactions");
                return;
            }

            // Get all groups for the user
            var groups = await this.groupService.GetUserGroupsAsync();

            // Collect all expenses from all groups
            this.allTransactions.Clear();
            foreach (var group in groups)
            {
                var expenses = await this.expenseService.GetGroupExpensesAsync(group.Id);
                foreach (var expense in expenses)
                {
                    // Get splits for this expense
                    var splits = await this.expenseService.GetExpenseSplitsAsync(expense.Id);
                    var userSplit = splits.FirstOrDefault(s => s.UserId == currentUser.Id);

                    if (userSplit != null)
                    {
                        var transaction = new TransactionViewModel
                        {
                            ExpenseId = expense.Id,
                            Description = expense.Description,
                            Amount = expense.TotalAmount,
                            Date = expense.ExpenseDate,
                            GroupName = group.Name,
                            GroupId = group.Id,
                            PaidByUserId = expense.PaidBy,
                            CurrentUserId = currentUser.Id,
                            YourShare = userSplit.Amount,
                        };

                        this.allTransactions.Add(transaction);
                    }
                }
            }

            // Apply initial filters
            await this.ApplyFiltersAsync();
        }
        catch (Exception ex)
        {
            this.errorService.HandleException(ex, "Failed to load transactions");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    private async Task ApplyFiltersAsync()
    {
        try
        {
            this.IsBusy = true;

            // Filter by date range
            var filtered = this.allTransactions
                .Where(t => t.Date.Date >= this.StartDate.Date && t.Date.Date <= this.EndDate.Date)
                .ToList();

            // Filter by transaction type
            filtered = this.selectedFilter switch
            {
                "Paid" => filtered.Where(t => t.PaidByUserId == t.CurrentUserId).ToList(),
                "Owe" => filtered.Where(t => t.PaidByUserId != t.CurrentUserId).ToList(),
                _ => filtered,
            };

            // Sort by date descending
            filtered = filtered.OrderByDescending(t => t.Date).ToList();

            // Update collection
            this.FilteredTransactions.Clear();
            foreach (var transaction in filtered)
            {
                this.FilteredTransactions.Add(transaction);
            }

            // Calculate summaries
            this.CalculateSummaries();
        }
        catch (Exception ex)
        {
            this.errorService.HandleException(ex, "Failed to apply filters");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    private void FilterByType(string? filterType)
    {
        if (filterType != null)
        {
            this.selectedFilter = filterType;
            this.UpdateFilterColors();
        }
    }

    private void UpdateFilterColors()
    {
        var activeColor = Color.FromArgb("#512BD4"); // Primary color
        var inactiveColor = Color.FromArgb("#888888"); // Gray

        this.FilterAllColor = this.selectedFilter == "All" ? activeColor : inactiveColor;
        this.FilterPaidColor = this.selectedFilter == "Paid" ? activeColor : inactiveColor;
        this.FilterOweColor = this.selectedFilter == "Owe" ? activeColor : inactiveColor;
    }

    private void CalculateSummaries()
    {
        this.TotalPaid = this.FilteredTransactions
            .Where(t => t.PaidByUserId == t.CurrentUserId)
            .Sum(t => t.Amount);

        this.TotalOwed = this.FilteredTransactions
            .Where(t => t.PaidByUserId != t.CurrentUserId)
            .Sum(t => t.YourShare);

        this.NetBalance = this.TotalPaid - this.TotalOwed;
    }
}

/// <summary>
/// View model for a single transaction in the history.
/// </summary>
public class TransactionViewModel
{
    /// <summary>
    /// Gets or sets the expense ID.
    /// </summary>
    public Guid ExpenseId { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount of the expense.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the date of the expense.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group ID.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who paid.
    /// </summary>
    public Guid PaidByUserId { get; set; }

    /// <summary>
    /// Gets or sets the current user ID.
    /// </summary>
    public Guid CurrentUserId { get; set; }

    /// <summary>
    /// Gets or sets the current user's share.
    /// </summary>
    public decimal YourShare { get; set; }

    /// <summary>
    /// Gets a value indicating whether the current user paid for this expense.
    /// </summary>
    public bool IsPaidByCurrentUser => this.PaidByUserId == this.CurrentUserId;

    /// <summary>
    /// Gets the display amount with appropriate sign.
    /// </summary>
    public string DisplayAmount => this.IsPaidByCurrentUser
        ? $"+${this.Amount:F2}"
        : $"-${this.YourShare:F2}";

    /// <summary>
    /// Gets the amount text color.
    /// </summary>
    public Color AmountTextColor => this.IsPaidByCurrentUser
        ? Color.FromArgb("#198754") // Success/Green
        : Color.FromArgb("#DC3545"); // Danger/Red

    /// <summary>
    /// Gets the transaction type label.
    /// </summary>
    public string TypeLabel => this.IsPaidByCurrentUser ? "You paid" : "You owe";

    /// <summary>
    /// Gets the transaction type text color.
    /// </summary>
    public Color TypeTextColor => this.IsPaidByCurrentUser
        ? Color.FromArgb("#198754")
        : Color.FromArgb("#DC3545");

    /// <summary>
    /// Gets the transaction type icon.
    /// </summary>
    public string TypeIcon => this.IsPaidByCurrentUser ? "💰" : "💸";

    /// <summary>
    /// Gets the transaction type background color.
    /// </summary>
    public Color TypeBackgroundColor => this.IsPaidByCurrentUser
        ? Color.FromArgb("#198754")
        : Color.FromArgb("#DC3545");
}
