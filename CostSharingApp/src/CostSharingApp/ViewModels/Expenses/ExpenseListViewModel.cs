// <copyright file="ExpenseListViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>


using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CostSharing.Core.Models;
using CostSharingApp.Services;

namespace CostSharingApp.ViewModels.Expenses;
/// <summary>
/// View model for expense list.
/// </summary>
public partial class ExpenseListViewModel : BaseViewModel
{
    private readonly IExpenseService expenseService;

    [ObservableProperty]
    private ObservableCollection<Expense> expenses = new();

    [ObservableProperty]
    private Guid groupId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseListViewModel"/> class.
    /// </summary>
    /// <param name="expenseService">Expense service.</param>
    public ExpenseListViewModel(IExpenseService expenseService)
    {
        this.expenseService = expenseService;
    }

    /// <summary>
    /// Loads expenses for the group.
    /// </summary>
    /// <returns>Task representing the async operation.</returns>
    public async Task LoadExpensesAsync()
    {
        if (this.GroupId == Guid.Empty)
        {
            return;
        }

        this.IsBusy = true;

        try
        {
            var expenses = await this.expenseService.GetGroupExpensesAsync(this.GroupId);

            this.Expenses.Clear();
            foreach (var expense in expenses)
            {
                this.Expenses.Add(expense);
            }
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    /// <summary>
    /// View expense command.
    /// </summary>
    /// <param name="expense">The expense to view.</param>
    [RelayCommand]
    private async Task ViewExpenseAsync(Expense expense)
    {
        await Shell.Current.GoToAsync($"expensedetails?expenseId={expense.Id}");
    }
}
