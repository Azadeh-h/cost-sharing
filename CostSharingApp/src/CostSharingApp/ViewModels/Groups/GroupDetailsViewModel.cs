using System.Collections.ObjectModel;
using System.Windows.Input;
using CostSharing.Core.Models;
using CostSharing.Core.Services;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels.Groups;

/// <summary>
/// ViewModel for group details page.
/// </summary>
public class GroupDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IGroupService groupService;
    private readonly IInvitationService invitationService;
    private readonly IAuthService authService;
    private readonly IErrorService errorService;
    private readonly IExpenseService expenseService;
    private readonly IDebtCalculationService debtCalculationService;
    private readonly ISettlementService settlementService;
    private Group? group;
    private ObservableCollection<GroupMember> members = new();
    private ObservableCollection<Invitation> pendingInvitations = new();
    private ObservableCollection<Expense> expenses = new();
    private ObservableCollection<Debt> debts = new();
    private ObservableCollection<Settlement> settlements = new();
    private string errorMessage = string.Empty;
    private bool isAdmin;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupDetailsViewModel"/> class.
    /// </summary>
    public GroupDetailsViewModel(
        IGroupService groupService,
        IInvitationService invitationService,
        IAuthService authService,
        IErrorService errorService,
        IExpenseService expenseService,
        IDebtCalculationService debtCalculationService,
        ISettlementService settlementService)
    {
        this.groupService = groupService;
        this.invitationService = invitationService;
        this.authService = authService;
        this.errorService = errorService;
        this.expenseService = expenseService;
        this.debtCalculationService = debtCalculationService;
        this.settlementService = settlementService;

        this.DeleteGroupCommand = new Command(async () => await this.DeleteGroupAsync(), () => this.isAdmin);
        this.EditGroupCommand = new Command(async () => await this.EditGroupAsync(), () => this.isAdmin);
        this.InviteMemberCommand = new Command(async () => await this.InviteMemberAsync(), () => this.isAdmin);
        this.AddExpenseCommand = new Command(async () => await this.AddExpenseAsync());
        this.RemoveMemberCommand = new Command<GroupMember>(async (member) => await this.RemoveMemberAsync(member), (_) => this.isAdmin);
        this.ResendInvitationCommand = new Command<Invitation>(async (inv) => await this.ResendInvitationAsync(inv), (_) => this.isAdmin);
        this.CancelInvitationCommand = new Command<Invitation>(async (inv) => await this.CancelInvitationAsync(inv), (_) => this.isAdmin);
        this.RefreshCommand = new Command(async () => await this.RefreshAsync());
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
    /// Gets the collection of pending invitations.
    /// </summary>
    public ObservableCollection<Invitation> PendingInvitations
    {
        get => this.pendingInvitations;
        set => this.SetProperty(ref this.pendingInvitations, value);
    }

    /// <summary>
    /// Gets the collection of members.
    /// </summary>
    public ObservableCollection<GroupMember> Members
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
    /// Gets the command to resend an invitation.
    /// </summary>
    public ICommand ResendInvitationCommand { get; }

    /// <summary>
    /// Gets the command to cancel an invitation.
    /// </summary>
    public ICommand CancelInvitationCommand { get; }

    /// <summary>
    /// Gets the command to add an expense.
    /// </summary>
    public ICommand AddExpenseCommand { get; }

    /// <summary>
    /// Gets the command to refresh group data.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Gets the collection of expenses.
    /// </summary>
    public ObservableCollection<Expense> Expenses
    {
        get => this.expenses;
        set => this.SetProperty(ref this.expenses, value);
    }

    /// <summary>
    /// Gets the collection of debts.
    /// </summary>
    public ObservableCollection<Debt> Debts
    {
        get => this.debts;
        set => this.SetProperty(ref this.debts, value);
    }

    /// <summary>
    /// Gets the collection of settlements.
    /// </summary>
    public ObservableCollection<Settlement> Settlements
    {
        get => this.settlements;
        set => this.SetProperty(ref this.settlements, value);
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
    /// Applies query attributes from navigation.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("groupId", out var groupIdObj) && groupIdObj is string groupIdString)
        {
            if (Guid.TryParse(groupIdString, out var groupId))
            {
                _ = this.LoadGroupAsync(groupId);
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

            this.Group = await this.groupService.GetGroupAsync(groupId);
            if (this.Group == null)
            {
                this.ErrorMessage = "Group not found.";
                return;
            }

            var members = await this.groupService.GetGroupMembersAsync(groupId);
            this.Members.Clear();
            foreach (var member in members)
            {
                this.Members.Add(member);
            }

            // Load pending invitations
            var invitations = await this.invitationService.GetPendingInvitationsAsync(groupId);
            this.PendingInvitations.Clear();
            foreach (var invitation in invitations)
            {
                this.PendingInvitations.Add(invitation);
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
        if (this.group == null)
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
            var success = await this.groupService.DeleteGroupAsync(this.group.Id);

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
    private async Task RemoveMemberAsync(GroupMember? member)
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

    /// <summary>
    /// Resends an invitation.
    /// </summary>
    private async Task ResendInvitationAsync(Invitation? invitation)
    {
        if (invitation == null)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            var success = await this.invitationService.ResendInvitationAsync(invitation.Id);

            if (success)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Invitation Resent",
                    $"Invitation resent to {invitation.InviteeContact}",
                    "OK");
            }
            else
            {
                this.ErrorMessage = "Failed to resend invitation.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "resending invitation");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Cancels an invitation.
    /// </summary>
    private async Task CancelInvitationAsync(Invitation? invitation)
    {
        if (invitation == null)
        {
            return;
        }

        bool confirmed = await Application.Current!.MainPage!.DisplayAlert(
            "Cancel Invitation",
            $"Cancel invitation to {invitation.InviteeContact}?",
            "Cancel Invitation",
            "Keep");

        if (!confirmed)
        {
            return;
        }

        try
        {
            this.IsBusy = true;
            var success = await this.invitationService.CancelInvitationAsync(invitation.Id);

            if (success)
            {
                this.PendingInvitations.Remove(invitation);
            }
            else
            {
                this.ErrorMessage = "Failed to cancel invitation.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = this.errorService.HandleException(ex, "cancelling invitation");
        }
        finally
        {
            this.IsBusy = false;
        }
    }
    private async Task EditGroupAsync()
    {
        if (this.group == null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"editgroup?groupId={this.group.Id}");
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

            this.Debts.Clear();
            foreach (var debt in debts)
            {
                this.Debts.Add(debt);
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
        await Shell.Current.GoToAsync($"simplifieddebts?groupId={this.groupId}");
    }

    /// <summary>
    /// Refreshes group data (expenses, settlements, debts).
    /// Called when returning from add expense or settlement recording.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    private async Task RefreshAsync()
    {
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
}
