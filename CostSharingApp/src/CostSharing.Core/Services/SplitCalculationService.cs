// <copyright file="SplitCalculationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>


using CostSharing.Core.Models;

namespace CostSharing.Core.Services;
/// <summary>
/// Service for calculating expense splits.
/// </summary>
public interface ISplitCalculationService
{
    /// <summary>
    /// Calculates even split among participants.
    /// </summary>
    /// <param name="totalAmount">Total amount to split.</param>
    /// <param name="participantIds">List of participant user IDs.</param>
    /// <returns>List of expense splits.</returns>
    List<ExpenseSplit> CalculateEvenSplit(decimal totalAmount, List<Guid> participantIds);

    /// <summary>
    /// Calculates custom percentage split.
    /// </summary>
    /// <param name="totalAmount">Total amount to split.</param>
    /// <param name="percentages">Dictionary of user ID to percentage (0-100).</param>
    /// <returns>List of expense splits.</returns>
    /// <exception cref="ArgumentException">If percentages don't sum to 100.</exception>
    List<ExpenseSplit> CalculateCustomSplit(decimal totalAmount, Dictionary<Guid, decimal> percentages);
}

/// <summary>
/// Implementation of split calculation service.
/// </summary>
public class SplitCalculationService : ISplitCalculationService
{
    /// <summary>
    /// Calculates even split among participants with proper rounding.
    /// </summary>
    /// <param name="totalAmount">Total amount to split.</param>
    /// <param name="participantIds">List of participant user IDs.</param>
    /// <returns>List of expense splits.</returns>
    public List<ExpenseSplit> CalculateEvenSplit(decimal totalAmount, List<Guid> participantIds)
    {
        if (participantIds == null || participantIds.Count == 0)
        {
            return new List<ExpenseSplit>();
        }

        var splits = new List<ExpenseSplit>();
        var participantCount = participantIds.Count;
        var percentage = 100m / participantCount;
        var equalShare = Math.Round(totalAmount / participantCount, 2, MidpointRounding.AwayFromZero);

        // Calculate total after rounding
        var totalAssigned = equalShare * participantCount;
        var difference = totalAmount - totalAssigned;

        // Add splits
        for (int i = 0; i < participantCount; i++)
        {
            var amount = equalShare;

            // Assign rounding difference to first participant
            if (i == 0)
            {
                amount += difference;
            }

            splits.Add(new ExpenseSplit
            {
                Id = Guid.NewGuid(),
                ExpenseId = Guid.Empty, // Will be set by ExpenseService
                UserId = participantIds[i],
                Percentage = Math.Round(percentage, 2),
                Amount = amount,
            });
        }

        return splits;
    }

    /// <summary>
    /// Calculates custom percentage split with validation.
    /// </summary>
    /// <param name="totalAmount">Total amount to split.</param>
    /// <param name="percentages">Dictionary of user ID to percentage (0-100).</param>
    /// <returns>List of expense splits.</returns>
    /// <exception cref="ArgumentException">If percentages don't sum to 100.</exception>
    public List<ExpenseSplit> CalculateCustomSplit(decimal totalAmount, Dictionary<Guid, decimal> percentages)
    {
        if (percentages == null || percentages.Count == 0)
        {
            return new List<ExpenseSplit>();
        }

        // Validate percentages sum to 100
        var totalPercentage = percentages.Values.Sum();
        if (Math.Abs(totalPercentage - 100m) > 0.01m)
        {
            throw new ArgumentException($"Percentages must sum to 100%, got {totalPercentage}%");
        }

        var splits = new List<ExpenseSplit>();
        var totalAssigned = 0m;

        // Calculate amounts based on percentages
        var orderedUsers = percentages.OrderByDescending(p => p.Value).ToList();

        for (int i = 0; i < orderedUsers.Count; i++)
        {
            var userId = orderedUsers[i].Key;
            var percentage = orderedUsers[i].Value;

            // Skip 0% participants
            if (percentage <= 0)
            {
                continue;
            }

            decimal amount;
            if (i == orderedUsers.Count - 1)
            {
                // Last participant gets remainder to handle rounding
                amount = totalAmount - totalAssigned;
            }
            else
            {
                amount = Math.Round((totalAmount * percentage) / 100m, 2, MidpointRounding.AwayFromZero);
                totalAssigned += amount;
            }

            splits.Add(new ExpenseSplit
            {
                Id = Guid.NewGuid(),
                ExpenseId = Guid.Empty, // Will be set by ExpenseService
                UserId = userId,
                Percentage = percentage,
                Amount = amount,
            });
        }

        return splits;
    }
}
