using CostSharing.Core.Models;

namespace CostSharing.Core.Algorithms;

/// <summary>
/// Implements the Min-Cash-Flow algorithm to minimize the number of transactions
/// needed to settle all debts in a group.
/// </summary>
public class DebtSimplificationAlgorithm
{
    /// <summary>
    /// Simplifies a list of debts into the minimum number of transactions needed.
    /// Uses a greedy approach: repeatedly match the maximum creditor with the maximum debtor.
    /// </summary>
    /// <param name="debts">List of all debts in the group.</param>
    /// <returns>List of simplified settlement transactions.</returns>
    public List<SimplifiedTransaction> SimplifyDebts(List<Debt> debts)
    {
        if (debts == null || debts.Count == 0)
        {
            return new List<SimplifiedTransaction>();
        }

        // Step 1: Calculate net balance for each user
        var netBalances = this.CalculateNetBalances(debts);

        // Step 2: Apply greedy matching algorithm
        return this.GreedyMatching(netBalances);
    }

    /// <summary>
    /// Calculates the net balance for each user (total owed - total owing).
    /// Positive balance = user is owed money (creditor).
    /// Negative balance = user owes money (debtor).
    /// </summary>
    /// <param name="debts">List of debts.</param>
    /// <returns>Dictionary mapping user ID to net balance.</returns>
    public Dictionary<Guid, decimal> CalculateNetBalances(List<Debt> debts)
    {
        var balances = new Dictionary<Guid, decimal>();

        foreach (var debt in debts)
        {
            // Creditor: owed money (positive balance)
            if (!balances.ContainsKey(debt.CreditorId))
            {
                balances[debt.CreditorId] = 0;
            }

            balances[debt.CreditorId] += debt.Amount;

            // Debtor: owes money (negative balance)
            if (!balances.ContainsKey(debt.DebtorId))
            {
                balances[debt.DebtorId] = 0;
            }

            balances[debt.DebtorId] -= debt.Amount;
        }

        return balances;
    }

    /// <summary>
    /// Greedy matching algorithm:
    /// 1. Find the user with maximum credit (most owed)
    /// 2. Find the user with maximum debt (owes most)
    /// 3. Settle the minimum of the two amounts
    /// 4. Update balances and repeat until all settled
    /// </summary>
    /// <param name="netBalances">Net balances for each user.</param>
    /// <returns>List of simplified transactions.</returns>
    public List<SimplifiedTransaction> GreedyMatching(Dictionary<Guid, decimal> netBalances)
    {
        var transactions = new List<SimplifiedTransaction>();
        var balances = new Dictionary<Guid, decimal>(netBalances);

        // Remove users with zero balance (already settled)
        var activeBalances = balances.Where(b => Math.Abs(b.Value) >= 0.01m).ToDictionary(b => b.Key, b => b.Value);

        while (activeBalances.Count > 0)
        {
            // Find max creditor (highest positive balance)
            var maxCreditor = activeBalances.OrderByDescending(b => b.Value).FirstOrDefault();
            if (maxCreditor.Value <= 0)
            {
                break; // No more creditors
            }

            // Find max debtor (most negative balance)
            var maxDebtor = activeBalances.OrderBy(b => b.Value).FirstOrDefault();
            if (maxDebtor.Value >= 0)
            {
                break; // No more debtors
            }

            // Calculate settlement amount (minimum of credit and debt)
            var settlementAmount = Math.Min(maxCreditor.Value, Math.Abs(maxDebtor.Value));
            settlementAmount = Math.Round(settlementAmount, 2);

            // Create transaction: debtor pays creditor
            transactions.Add(new SimplifiedTransaction
            {
                FromUserId = maxDebtor.Key,
                ToUserId = maxCreditor.Key,
                Amount = settlementAmount
            });

            // Update balances
            activeBalances[maxCreditor.Key] -= settlementAmount;
            activeBalances[maxDebtor.Key] += settlementAmount;

            // Remove users with zero balance
            activeBalances = activeBalances
                .Where(b => Math.Abs(b.Value) >= 0.01m)
                .ToDictionary(b => b.Key, b => b.Value);
        }

        return transactions;
    }

    /// <summary>
    /// Compares the number of transactions before and after simplification.
    /// </summary>
    /// <param name="originalDebts">Original debt list.</param>
    /// <param name="simplifiedTransactions">Simplified transaction list.</param>
    /// <returns>Summary of the simplification.</returns>
    public SimplificationSummary GetSimplificationSummary(List<Debt> originalDebts, List<SimplifiedTransaction> simplifiedTransactions)
    {
        return new SimplificationSummary
        {
            OriginalTransactionCount = originalDebts.Count,
            SimplifiedTransactionCount = simplifiedTransactions.Count,
            TransactionsSaved = originalDebts.Count - simplifiedTransactions.Count,
            TotalAmount = simplifiedTransactions.Sum(t => t.Amount)
        };
    }
}

/// <summary>
/// Represents a simplified settlement transaction between two users.
/// </summary>
public class SimplifiedTransaction
{
    /// <summary>
    /// Gets or sets the user ID who owes money (payer).
    /// </summary>
    public Guid FromUserId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who is owed money (receiver).
    /// </summary>
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Gets or sets the amount to be paid.
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Summary of debt simplification results.
/// </summary>
public class SimplificationSummary
{
    /// <summary>
    /// Gets or sets the original number of debt transactions.
    /// </summary>
    public int OriginalTransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the simplified number of transactions.
    /// </summary>
    public int SimplifiedTransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of transactions saved.
    /// </summary>
    public int TransactionsSaved { get; set; }

    /// <summary>
    /// Gets or sets the total amount involved in settlements.
    /// </summary>
    public decimal TotalAmount { get; set; }
}
