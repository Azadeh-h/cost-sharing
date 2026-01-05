// <copyright file="DebtCalculationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharing.Core.Services;

using CostSharing.Core.Models;

/// <summary>
/// Service for calculating debts from expenses.
/// </summary>
public interface IDebtCalculationService
{
    /// <summary>
    /// Calculates all debts for a group from expenses.
    /// </summary>
    /// <param name="expenses">List of expenses.</param>
    /// <param name="splits">List of all expense splits.</param>
    /// <returns>List of debts (who owes whom).</returns>
    List<Debt> CalculateDebts(List<Expense> expenses, List<ExpenseSplit> splits);

    /// <summary>
    /// Calculates all debts for a group from expenses, accounting for settlements.
    /// </summary>
    /// <param name="expenses">List of expenses.</param>
    /// <param name="splits">List of all expense splits.</param>
    /// <param name="settlements">List of recorded settlements.</param>
    /// <returns>List of debts (who owes whom) after subtracting settlements.</returns>
    List<Debt> CalculateDebts(List<Expense> expenses, List<ExpenseSplit> splits, List<Settlement> settlements);
}

/// <summary>
/// Implementation of debt calculation service.
/// </summary>
public class DebtCalculationService : IDebtCalculationService
{
    /// <summary>
    /// Calculates all debts for a group from expenses.
    /// For each expense, participants owe the payer their split amount (unless they are the payer).
    /// </summary>
    /// <param name="expenses">List of expenses.</param>
    /// <param name="splits">List of all expense splits.</param>
    /// <returns>List of debts (who owes whom).</returns>
    public List<Debt> CalculateDebts(List<Expense> expenses, List<ExpenseSplit> splits)
    {
        if (expenses == null || splits == null || expenses.Count == 0)
        {
            return new List<Debt>();
        }

        // Track net balances: positive means owed money, negative means owes money
        var balances = new Dictionary<Guid, decimal>();

        // Process each expense
        foreach (var expense in expenses)
        {
            var expenseSplits = splits.Where(s => s.ExpenseId == expense.Id).ToList();

            // Payer paid the full amount
            if (!balances.ContainsKey(expense.PaidBy))
            {
                balances[expense.PaidBy] = 0;
            }

            balances[expense.PaidBy] += expense.TotalAmount;

            // Each participant owes their split amount
            foreach (var split in expenseSplits)
            {
                if (!balances.ContainsKey(split.UserId))
                {
                    balances[split.UserId] = 0;
                }

                balances[split.UserId] -= split.Amount;
            }
        }

        // Convert balances to debts
        var debts = new List<Debt>();
        var creditors = balances.Where(b => b.Value > 0.01m).OrderByDescending(b => b.Value).ToList();
        var debtors = balances.Where(b => b.Value < -0.01m).OrderBy(b => b.Value).ToList();

        // Create debts from debtors to creditors
        foreach (var debtor in debtors)
        {
            var remainingDebt = Math.Abs(debtor.Value);

            foreach (var creditor in creditors)
            {
                if (remainingDebt < 0.01m)
                {
                    break;
                }

                var availableCredit = creditor.Value;
                if (availableCredit < 0.01m)
                {
                    continue;
                }

                var debtAmount = Math.Min(remainingDebt, availableCredit);

                debts.Add(new Debt
                {
                    Id = Guid.NewGuid(),
                    GroupId = expenses.First().GroupId,
                    DebtorId = debtor.Key,
                    CreditorId = creditor.Key,
                    Amount = Math.Round(debtAmount, 2),
                    CalculatedAt = DateTime.UtcNow,
                });

                remainingDebt -= debtAmount;
                balances[creditor.Key] -= debtAmount;
            }
        }

        return debts;
    }

    /// <summary>
    /// Calculates all debts for a group from expenses, accounting for settlements.
    /// For each expense, participants owe the payer their split amount (unless they are the payer).
    /// Then subtracts confirmed settlement amounts to show remaining debts.
    /// </summary>
    /// <param name="expenses">List of expenses.</param>
    /// <param name="splits">List of all expense splits.</param>
    /// <param name="settlements">List of recorded settlements.</param>
    /// <returns>List of debts (who owes whom) after subtracting settlements.</returns>
    public List<Debt> CalculateDebts(List<Expense> expenses, List<ExpenseSplit> splits, List<Settlement> settlements)
    {
        if (expenses == null || splits == null || expenses.Count == 0)
        {
            return new List<Debt>();
        }

        // Track net balances: positive means owed money, negative means owes money
        var balances = new Dictionary<Guid, decimal>();

        // Process each expense
        foreach (var expense in expenses)
        {
            var expenseSplits = splits.Where(s => s.ExpenseId == expense.Id).ToList();

            // Payer paid the full amount
            if (!balances.ContainsKey(expense.PaidBy))
            {
                balances[expense.PaidBy] = 0;
            }

            balances[expense.PaidBy] += expense.TotalAmount;

            // Each participant owes their split amount
            foreach (var split in expenseSplits)
            {
                if (!balances.ContainsKey(split.UserId))
                {
                    balances[split.UserId] = 0;
                }

                balances[split.UserId] -= split.Amount;
            }
        }

        // Apply settlements (confirmed settlements reduce debts)
        if (settlements != null && settlements.Any())
        {
            foreach (var settlement in settlements.Where(s => s.Status == SettlementStatus.Confirmed))
            {
                // Settlement: PaidBy paid PaidTo the amount
                // This reduces PaidBy's debt (or increases credit)
                if (!balances.ContainsKey(settlement.PaidBy))
                {
                    balances[settlement.PaidBy] = 0;
                }

                balances[settlement.PaidBy] -= settlement.Amount;

                // This increases PaidTo's debt (or reduces credit)
                if (!balances.ContainsKey(settlement.PaidTo))
                {
                    balances[settlement.PaidTo] = 0;
                }

                balances[settlement.PaidTo] += settlement.Amount;
            }
        }

        // Convert balances to debts
        var debts = new List<Debt>();
        var creditors = balances.Where(b => b.Value > 0.01m).OrderByDescending(b => b.Value).ToList();
        var debtors = balances.Where(b => b.Value < -0.01m).OrderBy(b => b.Value).ToList();

        // Create debts from debtors to creditors
        foreach (var debtor in debtors)
        {
            var remainingDebt = Math.Abs(debtor.Value);

            foreach (var creditor in creditors)
            {
                if (remainingDebt < 0.01m)
                {
                    break;
                }

                var availableCredit = creditor.Value;
                if (availableCredit < 0.01m)
                {
                    continue;
                }

                var debtAmount = Math.Min(remainingDebt, availableCredit);

                debts.Add(new Debt
                {
                    Id = Guid.NewGuid(),
                    GroupId = expenses.First().GroupId,
                    DebtorId = debtor.Key,
                    CreditorId = creditor.Key,
                    Amount = Math.Round(debtAmount, 2),
                    CalculatedAt = DateTime.UtcNow,
                });

                remainingDebt -= debtAmount;
                balances[creditor.Key] -= debtAmount;
            }
        }

        return debts;
    }
}
