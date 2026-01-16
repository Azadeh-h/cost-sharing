// <copyright file="IConflictResolver.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CostSharing.Core.Models;

namespace CostSharing.Core.Services;

/// <summary>
/// Interface for resolving sync conflicts.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Merges local and remote expenses, keeping both versions for conflicts.
    /// </summary>
    /// <param name="localExpenses">Local expenses.</param>
    /// <param name="remoteExpenses">Remote expenses.</param>
    /// <returns>Merged expense list.</returns>
    Task<List<Expense>> MergeExpensesAsync(List<Expense> localExpenses, List<Expense> remoteExpenses);

    /// <summary>
    /// Merges group metadata using last-write-wins strategy.
    /// </summary>
    /// <param name="localGroup">Local group.</param>
    /// <param name="remoteGroup">Remote group.</param>
    /// <returns>Merged group.</returns>
    Task<Group> MergeGroupMetadataAsync(Group localGroup, Group remoteGroup);

    /// <summary>
    /// Detects conflicts for a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <returns>List of conflict descriptions.</returns>
    Task<List<string>> DetectConflictsAsync(Guid groupId);

    /// <summary>
    /// Resolves conflicts by accepting remote version.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    Task ResolveConflictsByAcceptingRemoteAsync(Guid groupId);
}
