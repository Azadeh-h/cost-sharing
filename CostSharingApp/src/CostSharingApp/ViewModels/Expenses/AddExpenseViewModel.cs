// <copyright file="AddExpenseViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharingApp.ViewModels.Expenses;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CostSharing.Core.Models;
using CostSharing.Core.Services;
using CostSharingApp.Services;

/// <summary>
/// View model for adding expenses.
/// </summary>
public partial class AddExpenseViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IExpenseService expenseService;
    private readonly IGroupService groupService;
    private readonly ISplitCalculationService splitCalculationService;

    [ObservableProperty]
    private Guid groupId;

    [ObservableProperty]
    private Guid? expenseId;

    [ObservableProperty]
    private bool isEditMode = false;

    private Guid? originalCreatedBy; // Store original CreatedBy

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string amount = string.Empty;

    [ObservableProperty]
    private DateTime expenseDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<MemberSelectionItem> members = new();

    [ObservableProperty]
    private MemberSelectionItem? selectedPayer;

    [ObservableProperty]
    private bool isEvenSplit = true;

    [ObservableProperty]
    private bool isCustomSplit = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    private Dictionary<Guid, decimal>? customPercentages;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddExpenseViewModel"/> class.
    /// </summary>
    /// <param name="expenseService">Expense service.</param>
    /// <param name="groupService">Group service.</param>
    /// <param name="splitCalculationService">Split calculation service.</param>
    public AddExpenseViewModel(
        IExpenseService expenseService,
        IGroupService groupService,
        ISplitCalculationService splitCalculationService)
    {
        this.expenseService = expenseService;
        this.groupService = groupService;
        this.splitCalculationService = splitCalculationService;
    }

    /// <summary>
    /// Applies query parameters.
    /// </summary>
    /// <param name="query">Query dictionary.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("groupId", out var groupIdObj) && groupIdObj is string groupIdStr)
        {
            this.GroupId = Guid.Parse(groupIdStr);
            _ = this.LoadMembersAsync();
        }

        // Check if editing existing expense
        if (query.TryGetValue("expenseId", out var expenseIdObj) && expenseIdObj is string expenseIdStr)
        {
            this.ExpenseId = Guid.Parse(expenseIdStr);
            this.IsEditMode = true;
            _ = this.LoadExpenseAsync();
        }

        // Receive custom percentages from CustomSplitPage
        if (query.TryGetValue("customPercentages", out var percentagesObj) && percentagesObj is Dictionary<Guid, decimal> percentages)
        {
            this.customPercentages = percentages;
        }
    }

    /// <summary>
    /// Select all members command.
    /// </summary>
    [RelayCommand]
    private void SelectAllMembers()
    {
        foreach (var member in this.Members)
        {
            member.IsSelected = true;
        }
    }

    /// <summary>
    /// Navigate to custom split page command.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureCustomSplitAsync()
    {
        // Validation
        if (!decimal.TryParse(this.Amount, out var amountValue) || amountValue <= 0)
        {
            this.ErrorMessage = "Please enter a valid amount first";
            return;
        }

        var selectedMembers = this.Members.Where(m => m.IsSelected).ToList();
        if (selectedMembers.Count == 0)
        {
            this.ErrorMessage = "Please select at least one member";
            return;
        }

        // Navigate to custom split page
        var memberIdsStr = string.Join(",", selectedMembers.Select(m => m.UserId));
        await Shell.Current.GoToAsync($"customsplit?amount={amountValue}&memberIds={memberIdsStr}");
    }

    /// <summary>
    /// Add expense command.
    /// </summary>
    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        this.ErrorMessage = string.Empty;

        // Validation
        if (string.IsNullOrWhiteSpace(this.Description) || this.Description.Length < 1 || this.Description.Length > 200)
        {
            this.ErrorMessage = "Description must be 1-200 characters";
            return;
        }

        if (!decimal.TryParse(this.Amount, out var amountValue) || amountValue <= 0)
        {
            this.ErrorMessage = "Amount must be greater than 0";
            return;
        }

        if (this.SelectedPayer == null)
        {
            this.ErrorMessage = "Please select who paid";
            return;
        }

        var selectedMembers = this.Members.Where(m => m.IsSelected).ToList();
        if (selectedMembers.Count == 0)
        {
            this.ErrorMessage = "Please select at least one member to split with";
            return;
        }

        this.IsBusy = true;

        try
        {
            // Create expense
            var expense = new Expense
            {
                GroupId = this.GroupId,
                Description = this.Description,
                TotalAmount = amountValue,
                PaidBy = this.SelectedPayer.UserId,
                SplitType = this.IsEvenSplit ? SplitType.Even : SplitType.Custom,
                ExpenseDate = this.ExpenseDate,
            };
            
            // Preserve original CreatedBy when editing
            if (this.IsEditMode && this.originalCreatedBy.HasValue)
            {
                expense.CreatedBy = this.originalCreatedBy.Value;
            }

            // Calculate splits
            var participantIds = selectedMembers.Select(m => m.UserId).ToList();
            List<ExpenseSplit> splits;

            if (this.IsEvenSplit)
            {
                splits = this.splitCalculationService.CalculateEvenSplit(amountValue, participantIds);
            }
            else
            {
                // Use custom percentages
                if (this.customPercentages == null || this.customPercentages.Count == 0)
                {
                    this.ErrorMessage = "Please configure custom split percentages";
                    return;
                }

                splits = this.splitCalculationService.CalculateCustomSplit(amountValue, this.customPercentages);
            }

            // Save or update expense
            bool success;
            if (this.IsEditMode && this.ExpenseId.HasValue)
            {
                expense.Id = this.ExpenseId.Value;
                success = await this.expenseService.UpdateExpenseAsync(expense, splits);
            }
            else
            {
                success = await this.expenseService.CreateExpenseAsync(expense, splits);
            }

            if (success)
            {
                // Go back to previous page
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                this.ErrorMessage = this.IsEditMode ? "Failed to update expense" : "Failed to add expense";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Cancel command.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        // Go back to previous page
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Delete expense command.
    /// </summary>
    [RelayCommand]
    private async Task DeleteExpenseAsync()
    {
        if (!this.IsEditMode || !this.ExpenseId.HasValue)
        {
            return;
        }

        // Confirm deletion
        bool confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Delete Expense",
            "Are you sure you want to delete this expense? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        this.IsBusy = true;

        try
        {
            var success = await this.expenseService.DeleteExpenseAsync(this.ExpenseId.Value);

            if (success)
            {
                // Go back to previous page
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                this.ErrorMessage = "Failed to delete expense. You may not have permission.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Error deleting expense: {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Loads group members.
    /// </summary>
    /// <returns>Task representing the async operation.</returns>
    private async Task LoadMembersAsync()
    {
        this.IsBusy = true;

        try
        {
            var groupMembers = await this.groupService.GetGroupMembersAsync(this.GroupId);

            this.Members.Clear();
            foreach (var member in groupMembers)
            {
                this.Members.Add(new MemberSelectionItem
                {
                    UserId = member.UserId,
                    IsSelected = false,
                });
            }

            // Select first member as default payer
            if (this.Members.Count > 0)
            {
                this.SelectedPayer = this.Members[0];
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load members: {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Loads existing expense for editing.
    /// </summary>
    /// <returns>Task representing the async operation.</returns>
    private async Task LoadExpenseAsync()
    {
        if (!this.ExpenseId.HasValue)
        {
            return;
        }

        this.IsBusy = true;

        try
        {
            var expense = await this.expenseService.GetExpenseAsync(this.ExpenseId.Value);
            if (expense == null)
            {
                this.ErrorMessage = "Expense not found";
                return;
            }

            this.Description = expense.Description;
            this.Amount = expense.TotalAmount.ToString("F2");
            this.ExpenseDate = expense.ExpenseDate;
            this.IsEvenSplit = expense.SplitType == SplitType.Even;
            this.IsCustomSplit = expense.SplitType == SplitType.Custom;
            
            // Store original CreatedBy for updates
            this.originalCreatedBy = expense.CreatedBy;

            // Load members first
            await this.LoadMembersAsync();

            // Set the payer
            this.SelectedPayer = this.Members.FirstOrDefault(m => m.UserId == expense.PaidBy);

            // Load and set splits
            var splits = await this.expenseService.GetExpenseSplitsAsync(this.ExpenseId.Value);
            foreach (var split in splits)
            {
                var member = this.Members.FirstOrDefault(m => m.UserId == split.UserId);
                if (member != null)
                {
                    member.IsSelected = true;
                }
            }

            // If custom split, load percentages
            if (this.IsCustomSplit && splits.Any())
            {
                this.customPercentages = splits.ToDictionary(s => s.UserId, s => (s.Amount / expense.TotalAmount) * 100);
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load expense: {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
        }
    }
}

/// <summary>
/// Member selection item for expense split.
/// </summary>
public partial class MemberSelectionItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [ObservableProperty]
    private Guid userId;

    /// <summary>
    /// Gets or sets a value indicating whether the member is selected.
    /// </summary>
    [ObservableProperty]
    private bool isSelected;
}
