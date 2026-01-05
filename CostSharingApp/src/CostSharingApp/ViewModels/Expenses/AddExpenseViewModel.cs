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

            // Calculate splits
            var participantIds = selectedMembers.Select(m => m.UserId).ToList();
            var splits = this.splitCalculationService.CalculateEvenSplit(amountValue, participantIds);

            // Save expense
            var success = await this.expenseService.CreateExpenseAsync(expense, splits);

            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Expense added successfully", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                this.ErrorMessage = "Failed to add expense";
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
        await Shell.Current.GoToAsync("..");
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
