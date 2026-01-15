
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CostSharing.Core.Models;
using CostSharing.Core.Services;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels.Groups;
/// <summary>
/// ViewModel for group details page.
/// </summary>
public partial class GroupDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IGroupService groupService;
    private readonly IAuthService authService;
    private readonly IErrorService errorService;
    private readonly IExpenseService expenseService;
    private readonly IDebtCalculationService debtCalculationService;
    private readonly ISettlementService settlementService;
    private readonly IDriveSyncService? driveSyncService;
    private readonly ILoggingService loggingService;
    private Group? group;
    private ObservableCollection<MemberViewModel> members = new();
    private ObservableCollection<Expense> expenses = new();
    private ObservableCollection<DebtViewModel> debts = new();
    private ObservableCollection<Settlement> settlements = new();
    private string errorMessage = string.Empty;
    private bool isAdmin;
    private Guid? currentGroupId;
    private decimal userBalance;
    private Color userBalanceColor = Colors.Gray;
    private string userBalanceDescription = string.Empty;
    private bool isSyncEnabled;
    private string syncStatusText = "Not synced";
    private Color syncStatusColor = Colors.Gray;
    private string? lastSyncTime;
    private CancellationTokenSource? syncCancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupDetailsViewModel"/> class.
    /// </summary>
    public GroupDetailsViewModel(
        IGroupService groupService,
        IAuthService authService,
        IErrorService errorService,
        IExpenseService expenseService,
        IDebtCalculationService debtCalculationService,
        ISettlementService settlementService,
        IDriveSyncService driveSyncService,
        ILoggingService loggingService)
    {
        this.groupService = groupService;
        this.authService = authService;
        this.errorService = errorService;
        this.expenseService = expenseService;
        this.debtCalculationService = debtCalculationService;
        this.settlementService = settlementService;
        this.driveSyncService = driveSyncService;
        this.loggingService = loggingService;

        this.DeleteGroupCommand = new Command(async () => await this.DeleteGroupAsync(), () => this.isAdmin);
        this.EditGroupCommand = new Command(async () => await this.EditGroupAsync(), () => this.isAdmin);
        this.InviteMemberCommand = new Command(async () => await this.InviteMemberAsync(), () => this.isAdmin);
        this.AddExpenseCommand = new Command(async () => await this.AddExpenseAsync());
        this.EditExpenseCommand = new Command<Expense>(async (expense) => await this.EditExpenseAsync(expense));
        this.RemoveMemberCommand = new Command<MemberViewModel>(async (member) => await this.RemoveMemberAsync(member), (_) => this.isAdmin);
        this.RefreshCommand = new Command(async () => await this.RefreshAsync());
        this.SyncGroupCommand = new Command(async () => await this.SyncNowAsync(), () => this.isSyncEnabled);
        this.OpenSyncSettingsCommand = new Command(async () => await this.OpenSyncSettingsAsync());
        this.SendGmailInvitationCommand = new Command(async () => await this.SendGmailInvitationAsync(), () => this.isAdmin);
    }

    /// <summary>
    /// Gets or sets the group.
    /// </summary>
    public Group? Group
    {
        get => this.group;
        set
        {
            if (this.SetProperty(ref this.group, value))
            {
                this.Title = value?.Name ?? "Group Details";
            }
        }
    }

    /// <summary>
    /// Gets the collection of group members with user details.
    /// </summary>
    public ObservableCollection<MemberViewModel> Members
    {
        get => this.members;
        set => this.SetProperty(ref this.members, value);
    }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage
    {
        get => this.errorMessage;
        set => this.SetProperty(ref this.errorMessage, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the current user is an admin.
    /// </summary>
    public bool IsAdmin
    {
        get => this.isAdmin;
        set
        {
            if (this.SetProperty(ref this.isAdmin, value))
            {
                ((Command)this.DeleteGroupCommand).ChangeCanExecute();
                ((Command)this.EditGroupCommand).ChangeCanExecute();
                ((Command)this.InviteMemberCommand).ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Gets the command to delete the group.
    /// </summary>
    public ICommand DeleteGroupCommand { get; }

    /// <summary>
    /// Gets the command to remove a member.
    /// </summary>
    public ICommand RemoveMemberCommand { get; }

    /// <summary>
    /// Gets the command to add an expense.
    /// </summary>
    public ICommand AddExpenseCommand { get; }

    /// <summary>
    /// Gets the command to edit an expense.
    /// </summary>
    public ICommand EditExpenseCommand { get; }

    /// <summary>
    /// Gets the command to refresh group data.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Gets or sets the collection of expenses.
    /// </summary>
    public ObservableCollection<Expense> Expenses
    {
        get => this.expenses;
        set => this.SetProperty(ref this.expenses, value);
    }

    /// <summary>
    /// Gets the collection of debts.
    /// </summary>
    public ObservableCollection<DebtViewModel> Debts
    {
        get => this.debts;
        set => this.SetProperty(ref this.debts, value);
    }

    /// <summary>
    /// Gets or sets the user's balance in the group.
    /// </summary>
    public decimal UserBalance
    {
        get => this.userBalance;
        set => this.SetProperty(ref this.userBalance, value);
    }

    /// <summary>
    /// Gets or sets the color for the user's balance display.
    /// </summary>
    public Color UserBalanceColor
    {
        get => this.userBalanceColor;
        set => this.SetProperty(ref this.userBalanceColor, value);
    }

    /// <summary>
    /// Gets or sets the description for the user's balance.
    /// </summary>
    public string UserBalanceDescription
    {
        get => this.userBalanceDescription;
        set => this.SetProperty(ref this.userBalanceDescription, value);
    }

    /// <summary>
    /// Gets or sets the collection of settlements.
    /// </summary>
    public ObservableCollection<Settlement> Settlements
    {
        get => this.settlements;
        set => this.SetProperty(ref this.settlements, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether sync is enabled for this group.
    /// </summary>
    public bool IsSyncEnabled
    {
        get => this.isSyncEnabled;
        set
        {
            if (this.SetProperty(ref this.isSyncEnabled, value))
            {
                _ = this.OnSyncEnabledChangedAsync(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the sync status text.
    /// </summary>
    public string SyncStatusText
    {
        get => this.syncStatusText;
        set => this.SetProperty(ref this.syncStatusText, value);
    }

    /// <summary>
    /// Gets or sets the sync status color.
    /// </summary>
    public Color SyncStatusColor
    {
        get => this.syncStatusColor;
        set => this.SetProperty(ref this.syncStatusColor, value);
    }

    /// <summary>
    /// Gets or sets the last sync time display text.
    /// </summary>
    public string? LastSyncTime
    {
        get => this.lastSyncTime;
        set => this.SetProperty(ref this.lastSyncTime, value);
    }

    /// <summary>
    /// Gets the command to edit the group.
    /// </summary>
    public ICommand EditGroupCommand { get; }

    /// <summary>
    /// Gets the command to invite a member.
    /// </summary>
    public ICommand InviteMemberCommand { get; }

    /// <summary>
    /// Gets the command to sync this group with Google Drive.
    /// </summary>
    public ICommand SyncGroupCommand { get; }

    /// <summary>
    /// Gets the command to open sync settings.
    /// </summary>
    public ICommand OpenSyncSettingsCommand { get; }

    /// <summary>
    /// Gets the command to send Gmail invitation.
    /// </summary>
    public ICommand SendGmailInvitationCommand { get; }

    /// <summary>
    /// Applies query attributes from navigation.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("groupId", out var groupIdObj))
        {
            Guid groupId;

            // Handle both string and Guid types
            if (groupIdObj is string groupIdString && Guid.TryParse(groupIdString, out groupId))
            {
                this.currentGroupId = groupId;
                _ = this.LoadGroupAsync(groupId);
            }
            else if (groupIdObj is Guid guidValue)
            {
                this.currentGroupId = guidValue;
                _ = this.LoadGroupAsync(guidValue);
            }
        }
    }

    /// <summary>
    /// Loads the group details.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Task for async operation.</returns>
    private async Task LoadGroupAsync(Guid groupId)
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            this.ErrorMessage = string.Empty;

            // Clear all previous data first
            this.Group = null;
            this.Members.Clear();
            this.Expenses.Clear();
            this.Debts.Clear();
            this.Settlements.Clear();

            // Load the new group
            this.Group = await this.groupService.GetGroupAsync(groupId);
            if (this.Group == null)
            {
                this.ErrorMessage = "Group not found.";
                return;
            }

            // Force UI update
            this.OnPropertyChanged(nameof(Group));

            // Clean up duplicate unused users (e.g., multiple "Sarah Chen" where one isn't in any group)
            await this.authService.RemoveDuplicateUnusedUsersAsync();

            var members = await this.groupService.GetGroupMembersAsync(groupId);
            this.Members.Clear();

            // Get all users to lookup names
            var allUsers = new Dictionary<Guid, User>();
            foreach (var member in members)
            {
                var user = await this.authService.GetUserByIdAsync(member.UserId);
                if (user != null)
                {
                    allUsers[user.Id] = user;
                }
            }

            // Create MemberViewModels with user names
            foreach (var member in members)
            {
                var addedByUser = allUsers.ContainsKey(member.AddedBy) ? allUsers[member.AddedBy] : await this.authService.GetUserByIdAsync(member.AddedBy);
                
                var memberVm = new MemberViewModel
                {
                    Id = member.Id,
                    UserId = member.UserId,
                    UserName = allUsers.ContainsKey(member.UserId) ? allUsers[member.UserId].Name : "Unknown User",
                    Email = allUsers.ContainsKey(member.UserId) ? allUsers[member.UserId].Email : string.Empty,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt,
                    AddedBy = member.AddedBy,
                    AddedByName = addedByUser?.Name ?? "Unknown Admin",
                };
                this.Members.Add(memberVm);
            }

            // Load expenses
            var expenses = await this.expenseService.GetGroupExpensesAsync(groupId);
            this.Expenses.Clear();
            foreach (var expense in expenses)
            {
                this.Expenses.Add(expense);
            }

            // Calculate debts
            await this.CalculateDebtsAsync();

            // Load settlements
            var settlements = await this.settlementService.GetGroupSettlementsAsync(groupId);
            this.Settlements.Clear();
            foreach (var settlement in settlements)
            {
                this.Settlements.Add(settlement);
            }

            // Check if current user is admin
            var currentUser = this.authService.GetCurrentUser();
            this.IsAdmin = members.Any(m => m.UserId == currentUser?.Id && m.Role == GroupRole.Admin);
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "loading group details");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Deletes the group after confirmation.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task DeleteGroupAsync()
    {
        if (this.group == null || !this.currentGroupId.HasValue)
        {
            return;
        }

        bool confirmed = await Application.Current!.MainPage!.DisplayAlert(
            "Delete Group",
            $"Are you sure you want to delete '{this.group.Name}'? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            System.Diagnostics.Debug.WriteLine($"[GroupDetailsViewModel] Attempting to delete group: {this.currentGroupId.Value}");
            var success = await this.groupService.DeleteGroupAsync(this.currentGroupId.Value);

            System.Diagnostics.Debug.WriteLine($"[GroupDetailsViewModel] Delete result: {success}");
            if (success)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                this.ErrorMessage = "Failed to delete group. Please try again.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "deleting group");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Navigates to edit group page.
    /// </summary>
    /// <returns>Task for async operation.</returns>

    /// <summary>
    /// Removes a member from the group.
    /// </summary>
    private async Task RemoveMemberAsync(MemberViewModel? member)
    {
        if (member == null || this.group == null)
        {
            return;
        }

        var currentUser = this.authService.GetCurrentUser();
        if (currentUser == null)
        {
            return;
        }

        // Cannot remove yourself if you're the only admin
        var admins = this.Members.Where(m => m.Role == GroupRole.Admin).ToList();
        if (member.UserId == currentUser.Id && admins.Count == 1)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Cannot Remove",
                "You are the only admin. Please promote another member to admin first.",
                "OK");
            return;
        }

        bool confirmed = await Application.Current!.MainPage!.DisplayAlert(
            "Remove Member",
            $"Remove this member from '{this.group.Name}'?",
            "Remove",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            await this.groupService.RemoveMemberAsync(this.group.Id, member.UserId);
            this.Members.Remove(member);
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "removing member");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    private async Task EditGroupAsync()
    {
        if (this.group == null || !this.currentGroupId.HasValue)
        {
            return;
        }

        await Shell.Current.GoToAsync($"editgroup?groupId={this.currentGroupId.Value}");
    }

    /// <summary>
    /// Navigates to invite member page.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task InviteMemberAsync()
    {
        if (this.group == null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"invitemember?groupId={this.group.Id}");
    }

    /// <summary>
    /// Navigates to add expense page.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task AddExpenseAsync()
    {
        if (this.group == null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"addexpense?groupId={this.group.Id}");
    }

    /// <summary>
    /// Navigates to edit expense page.
    /// </summary>
    /// <param name="expense">Expense to edit.</param>
    /// <returns>Task for async operation.</returns>
    private async Task EditExpenseAsync(Expense expense)
    {
        if (this.group == null || expense == null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"addexpense?groupId={this.group.Id}&expenseId={expense.Id}");
    }

    /// <summary>
    /// Calculates debts for the group.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task CalculateDebtsAsync()
    {
        if (this.group == null)
        {
            return;
        }

        try
        {
            var expenses = this.Expenses.ToList();
            var allSplits = new List<ExpenseSplit>();

            foreach (var expense in expenses)
            {
                var splits = await this.expenseService.GetExpenseSplitsAsync(expense.Id);
                allSplits.AddRange(splits);
            }

            // Get settlements and pass them to debt calculation
            var settlements = this.Settlements.ToList();
            var debts = this.debtCalculationService.CalculateDebts(expenses, allSplits, settlements);

            // Get all users to map IDs to names
            var allUsers = await this.authService.GetAllUsersAsync();
            var userMap = allUsers.ToDictionary(u => u.Id, u => u.Name);

            // Convert to DebtViewModels with user names
            this.Debts.Clear();
            foreach (var debt in debts)
            {
                var debtVm = new DebtViewModel
                {
                    Id = debt.Id,
                    DebtorId = debt.DebtorId,
                    DebtorName = userMap.ContainsKey(debt.DebtorId) ? userMap[debt.DebtorId] : "Unknown User",
                    CreditorId = debt.CreditorId,
                    CreditorName = userMap.ContainsKey(debt.CreditorId) ? userMap[debt.CreditorId] : "Unknown User",
                    Amount = debt.Amount,
                    GroupId = debt.GroupId,
                };
                this.Debts.Add(debtVm);
            }

            // Calculate current user's balance
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser != null)
            {
                decimal balance = 0;
                
                // Sum up what others owe this user (positive)
                balance += debts.Where(d => d.CreditorId == currentUser.Id).Sum(d => d.Amount);
                
                // Subtract what this user owes others (negative)
                balance -= debts.Where(d => d.DebtorId == currentUser.Id).Sum(d => d.Amount);

                this.UserBalance = Math.Abs(balance);
                
                if (balance > 0)
                {
                    this.UserBalanceColor = Colors.Green;
                    this.UserBalanceDescription = "You are owed";
                }
                else if (balance < 0)
                {
                    this.UserBalanceColor = Colors.Red;
                    this.UserBalanceDescription = "You owe";
                }
                else
                {
                    this.UserBalanceColor = Colors.Gray;
                    this.UserBalanceDescription = "Settled up";
                }
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "calculating debts");
        }
    }

    /// <summary>
    /// Navigates to the simplified debts page.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    [RelayCommand]
    private async Task ViewSimplifiedDebtsAsync()
    {
        await Shell.Current.GoToAsync($"simplifieddebts?groupId={this.group?.Id}");
    }

    /// <summary>
    /// Refreshes group data (expenses, settlements, debts).
    /// Called when returning from add expense or settlement recording.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task RefreshAsync()
    {
        // If we have a stored group ID, reload from scratch
        if (this.currentGroupId.HasValue)
        {
            await this.LoadGroupAsync(this.currentGroupId.Value);
            return;
        }

        if (this.group == null || this.IsBusy)
        {
            return;
        }

        try
        {
            // Reload expenses
            var expenses = await this.expenseService.GetGroupExpensesAsync(this.group.Id);
            this.Expenses.Clear();
            foreach (var expense in expenses)
            {
                this.Expenses.Add(expense);
            }

            // Reload settlements
            var settlements = await this.settlementService.GetGroupSettlementsAsync(this.group.Id);
            this.Settlements.Clear();
            foreach (var settlement in settlements)
            {
                this.Settlements.Add(settlement);
            }

            // Recalculate debts (will account for new expenses and settlements)
            await this.CalculateDebtsAsync();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "refreshing group data");
        }
    }

    /// <summary>
    /// Handles sync enabled/disabled toggle.
    /// </summary>
    /// <param name="enabled">Whether sync is enabled.</param>
    /// <returns>Task for async operation.</returns>
    private async Task OnSyncEnabledChangedAsync(bool enabled)
    {
        if (this.group == null || this.driveSyncService == null)
        {
            return;
        }

        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                this.ErrorMessage = "User not authenticated";
                this.IsSyncEnabled = false;
                return;
            }

            if (enabled)
            {
                // Check if user is authorized for Drive
                var accessToken = await SecureStorage.GetAsync($"drive_access_token_{currentUser.Id}");
                if (string.IsNullOrEmpty(accessToken))
                {
                    // Need to authorize first
                    await Shell.Current.DisplayAlert("Authorization Required", 
                        "Please authorize Google Drive access to enable sync.", "OK");
                    this.IsSyncEnabled = false;
                    return;
                }

                // Create Drive folder if not exists
                if (string.IsNullOrEmpty(this.group.DriveFolderId))
                {
                    this.SyncStatusText = "Creating Drive folder...";
                    this.SyncStatusColor = Colors.Orange;

                    var folderId = await this.driveSyncService.CreateGroupFolderAsync(
                        this.group.Id, 
                        currentUser.Id);

                    // Grant access to all members
                    var members = await this.groupService.GetGroupMembersAsync(this.group.Id);
                    var memberEmails = new List<string>();
                    foreach (var member in members)
                    {
                        var user = await this.authService.GetUserByIdAsync(member.UserId);
                        if (user != null && user.Id != currentUser.Id)
                        {
                            memberEmails.Add(user.Email);
                        }
                    }

                    if (memberEmails.Any())
                    {
                        await this.driveSyncService.SetFolderPermissionsAsync(
                            folderId, 
                            memberEmails, 
                            currentUser.Id);
                    }
                }

                // Start periodic sync
                this.syncCancellationTokenSource = new CancellationTokenSource();
                _ = this.driveSyncService.StartPeriodicSyncAsync(
                    this.group.Id, 
                    currentUser.Id, 
                    this.syncCancellationTokenSource.Token);

                this.IsSyncEnabled = true;
                ((Command)this.SyncGroupCommand).ChangeCanExecute();
                this.SyncStatusText = "Sync enabled";
                this.SyncStatusColor = Colors.Green;
            }
            else
            {
                // Stop sync
                this.syncCancellationTokenSource?.Cancel();
                this.syncCancellationTokenSource = null;

                this.IsSyncEnabled = false;
                ((Command)this.SyncGroupCommand).ChangeCanExecute();
                this.SyncStatusText = "Sync disabled";
                this.SyncStatusColor = Colors.Gray;
            }
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to toggle sync", ex);
            this.ErrorMessage = "Failed to toggle sync. Please try again.";
            this.IsSyncEnabled = false;
        }
    }

    /// <summary>
    /// Manually trigger sync now.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task SyncNowAsync()
    {
        if (this.group == null || this.driveSyncService == null || !this.IsSyncEnabled)
        {
            return;
        }

        try
        {
            this.SyncStatusText = "Syncing...";
            this.SyncStatusColor = Colors.Orange;

            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                throw new InvalidOperationException("User not authenticated");
            }

            // Upload local changes
            await this.driveSyncService.UploadGroupDataAsync(this.group.Id, currentUser.Id);

            // Download remote changes
            await this.driveSyncService.DownloadGroupDataAsync(this.group.Id, currentUser.Id);

            // Refresh UI
            await this.RefreshAsync();

            this.SyncStatusText = "Synced";
            this.SyncStatusColor = Colors.Green;
            this.LastSyncTime = DateTime.Now.ToString("HH:mm");

            this.loggingService.LogInfo($"Manual sync completed for group {this.group.Id}");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Manual sync failed", ex);
            this.SyncStatusText = "Sync failed";
            this.SyncStatusColor = Colors.Red;
            
            // Show more specific error message
            if (ex.Message.Contains("not authorized") || ex.Message.Contains("re-authorize"))
            {
                this.ErrorMessage = "Authorization expired. Please go to Settings and re-authorize Google Drive.";
            }
            else
            {
                this.ErrorMessage = $"Sync failed: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Opens the sync settings page.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task OpenSyncSettingsAsync()
    {
        if (this.group == null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"syncsettings?groupId={this.group.Id}");
    }

    /// <summary>
    /// Sends Gmail invitation to new member.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task SendGmailInvitationAsync()
    {
        // TODO: Implement Gmail invitation with Drive folder link
        await Shell.Current.DisplayAlert("Coming Soon", 
            "Gmail invitations with Drive folder access will be available soon.", "OK");
    }
}
