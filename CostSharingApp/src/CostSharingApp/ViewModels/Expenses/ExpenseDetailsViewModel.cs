// <copyright file="ExpenseDetailsViewModel.cs" company="PlaceholderCompany">
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
/// View model for expense details.
/// </summary>
public partial class ExpenseDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IExpenseService expenseService;
    private readonly IAuthService authService;

    [ObservableProperty]
    private Expense expense = new();

    [ObservableProperty]
    private ObservableCollection<ExpenseSplit> splits = new();

    [ObservableProperty]
    private bool isCreator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseDetailsViewModel"/> class.
    /// </summary>
    /// <param name="expenseService">Expense service.</param>
    /// <param name="authService">Auth service.</param>
    public ExpenseDetailsViewModel(
        IExpenseService expenseService,
        IAuthService authService)
    {
        this.expenseService = expenseService;
        this.authService = authService;
    }

    /// <summary>
    /// Applies query parameters.
    /// </summary>
    /// <param name="query">Query dictionary.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("expenseId", out var expenseIdObj) && expenseIdObj is string expenseIdStr)
        {
            var expenseId = Guid.Parse(expenseIdStr);
            _ = this.LoadExpenseAsync(expenseId);
        }
    }

    /// <summary>
    /// Edit expense command.
    /// </summary>
    [RelayCommand]
    private async Task EditExpenseAsync()
    {
        // TODO: Navigate to edit page (Phase 6 or later)
        await Shell.Current.DisplayAlert("Info", "Edit functionality coming soon", "OK");
    }

    /// <summary>
    /// Delete expense command.
    /// </summary>
    [RelayCommand]
    private async Task DeleteExpenseAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Confirm Delete",
            "Are you sure you want to delete this expense?",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        this.IsBusy = true;

        try
        {
            var success = await this.expenseService.DeleteExpenseAsync(this.Expense.Id);

            if (success)
            {
                await Shell.Current.DisplayAlert("Success", "Expense deleted successfully", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to delete expense", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to delete expense: {ex.Message}", "OK");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// Loads expense details.
    /// </summary>
    /// <param name="expenseId">Expense ID.</param>
    /// <returns>Task representing the async operation.</returns>
    private async Task LoadExpenseAsync(Guid expenseId)
    {
        this.IsBusy = true;

        try
        {
            var expense = await this.expenseService.GetExpenseAsync(expenseId);
            if (expense != null)
            {
                this.Expense = expense;

                var splits = await this.expenseService.GetExpenseSplitsAsync(expenseId);
                this.Splits.Clear();
                foreach (var split in splits)
                {
                    this.Splits.Add(split);
                }

                // Check if current user is creator
                var currentUser = this.authService.GetCurrentUser();
                this.IsCreator = currentUser != null && this.Expense.CreatedBy == currentUser.Id;
            }
        }
        finally
        {
            this.IsBusy = false;
        }
    }
}
