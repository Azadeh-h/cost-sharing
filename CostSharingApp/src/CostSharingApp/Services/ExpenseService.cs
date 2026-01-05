// <copyright file="ExpenseService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharingApp.Services;

using CostSharing.Core.Models;
using CostSharing.Core.Services;

/// <summary>
/// Service for managing expenses.
/// </summary>
public interface IExpenseService
{
    /// <summary>
    /// Creates a new expense with splits.
    /// </summary>
    /// <param name="expense">The expense to create.</param>
    /// <param name="splits">The expense splits.</param>
    /// <returns>True if successful.</returns>
    Task<bool> CreateExpenseAsync(Expense expense, List<ExpenseSplit> splits);

    /// <summary>
    /// Gets all expenses for a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>List of expenses.</returns>
    Task<List<Expense>> GetGroupExpensesAsync(Guid groupId);

    /// <summary>
    /// Gets expense by ID.
    /// </summary>
    /// <param name="expenseId">Expense ID.</param>
    /// <returns>The expense or null.</returns>
    Task<Expense?> GetExpenseAsync(Guid expenseId);

    /// <summary>
    /// Gets splits for an expense.
    /// </summary>
    /// <param name="expenseId">Expense ID.</param>
    /// <returns>List of expense splits.</returns>
    Task<List<ExpenseSplit>> GetExpenseSplitsAsync(Guid expenseId);

    /// <summary>
    /// Updates an expense and its splits.
    /// </summary>
    /// <param name="expense">The expense to update.</param>
    /// <param name="splits">The new expense splits.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateExpenseAsync(Expense expense, List<ExpenseSplit> splits);

    /// <summary>
    /// Deletes an expense and its splits.
    /// </summary>
    /// <param name="expenseId">Expense ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteExpenseAsync(Guid expenseId);
}

/// <summary>
/// Implementation of expense service with cache and Drive sync.
/// </summary>
public class ExpenseService : IExpenseService
{
    private readonly ICacheService cacheService;
    private readonly IDriveService driveService;
    private readonly ILoggingService loggingService;
    private readonly IAuthService authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseService"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service.</param>
    /// <param name="driveService">Drive service.</param>
    /// <param name="loggingService">Logging service.</param>
    /// <param name="authService">Auth service.</param>
    public ExpenseService(
        ICacheService cacheService,
        IDriveService driveService,
        ILoggingService loggingService,
        IAuthService authService)
    {
        this.cacheService = cacheService;
        this.driveService = driveService;
        this.loggingService = loggingService;
        this.authService = authService;
    }

    /// <summary>
    /// Creates a new expense with splits and syncs to Drive.
    /// </summary>
    /// <param name="expense">The expense to create.</param>
    /// <param name="splits">The expense splits.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> CreateExpenseAsync(Expense expense, List<ExpenseSplit> splits)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return false;
            }

            // Set expense ID and metadata
            expense.Id = Guid.NewGuid();
            expense.CreatedBy = currentUser.Id;
            expense.CreatedAt = DateTime.UtcNow;

            // Set expense ID on splits
            foreach (var split in splits)
            {
                split.ExpenseId = expense.Id;
            }

            // Save to cache
            await this.cacheService.SaveAsync(expense);
            foreach (var split in splits)
            {
                await this.cacheService.SaveAsync(split);
            }

            // Sync to Google Drive
            await this.SyncExpensesToDrive(expense.GroupId);

            this.loggingService.LogInfo($"Created expense {expense.Id} in group {expense.GroupId}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to create expense", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets all expenses for a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>List of expenses.</returns>
    public async Task<List<Expense>> GetGroupExpensesAsync(Guid groupId)
    {
        try
        {
            var allExpenses = await this.cacheService.GetAllAsync<Expense>();
            return allExpenses.Where(e => e.GroupId == groupId)
                .OrderByDescending(e => e.ExpenseDate)
                .ToList();
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to get expenses for group {groupId}", ex);
            return new List<Expense>();
        }
    }

    /// <summary>
    /// Gets expense by ID.
    /// </summary>
    /// <param name="expenseId">Expense ID.</param>
    /// <returns>The expense or null.</returns>
    public async Task<Expense?> GetExpenseAsync(Guid expenseId)
    {
        try
        {
            return await this.cacheService.GetAsync<Expense>(expenseId);
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to get expense {expenseId}", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets splits for an expense.
    /// </summary>
    /// <param name="expenseId">Expense ID.</param>
    /// <returns>List of expense splits.</returns>
    public async Task<List<ExpenseSplit>> GetExpenseSplitsAsync(Guid expenseId)
    {
        try
        {
            var allSplits = await this.cacheService.GetAllAsync<ExpenseSplit>();
            return allSplits.Where(s => s.ExpenseId == expenseId).ToList();
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to get splits for expense {expenseId}", ex);
            return new List<ExpenseSplit>();
        }
    }

    /// <summary>
    /// Updates an expense and its splits.
    /// </summary>
    /// <param name="expense">The expense to update.</param>
    /// <param name="splits">The new expense splits.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> UpdateExpenseAsync(Expense expense, List<ExpenseSplit> splits)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null || expense.CreatedBy != currentUser.Id)
            {
                this.loggingService.LogWarning($"User {currentUser?.Id} not authorized to update expense {expense.Id}");
                return false;
            }

            // Delete old splits
            var oldSplits = await this.GetExpenseSplitsAsync(expense.Id);
            foreach (var oldSplit in oldSplits)
            {
                await this.cacheService.DeleteAsync(oldSplit);
            }

            // Save updated expense and new splits
            await this.cacheService.SaveAsync(expense);
            foreach (var split in splits)
            {
                split.ExpenseId = expense.Id;
                await this.cacheService.SaveAsync(split);
            }

            // Sync to Google Drive
            await this.SyncExpensesToDrive(expense.GroupId);

            this.loggingService.LogInfo($"Updated expense {expense.Id}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to update expense", ex);
            return false;
        }
    }

    /// <summary>
    /// Deletes an expense and its splits.
    /// </summary>
    /// <param name="expenseId">Expense ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> DeleteExpenseAsync(Guid expenseId)
    {
        try
        {
            var expense = await this.GetExpenseAsync(expenseId);
            if (expense == null)
            {
                return false;
            }

            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null || expense.CreatedBy != currentUser.Id)
            {
                this.loggingService.LogWarning($"User {currentUser?.Id} not authorized to delete expense {expenseId}");
                return false;
            }

            // Delete splits first
            var splits = await this.GetExpenseSplitsAsync(expenseId);
            foreach (var split in splits)
            {
                await this.cacheService.DeleteAsync(split);
            }

            // Delete expense
            await this.cacheService.DeleteAsync(expense);

            // Sync to Google Drive
            await this.SyncExpensesToDrive(expense.GroupId);

            this.loggingService.LogInfo($"Deleted expense {expenseId}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to delete expense", ex);
            return false;
        }
    }

    /// <summary>
    /// Syncs expenses and splits to Google Drive.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Task representing the async operation.</returns>
    private async Task SyncExpensesToDrive(Guid groupId)
    {
        try
        {
            var expenses = await this.GetGroupExpensesAsync(groupId);
            var allSplits = await this.cacheService.GetAllAsync<ExpenseSplit>();
            var groupSplits = allSplits.Where(s => expenses.Any(e => e.Id == s.ExpenseId)).ToList();

            await this.driveService.SaveDataAsync($"expenses_{groupId}.json", expenses);
            await this.driveService.SaveDataAsync($"splits_{groupId}.json", groupSplits);
        }
        catch (Exception ex)
        {
            this.loggingService.LogWarning($"Failed to sync expenses to Drive for group {groupId}", ex);
        }
    }
}
