using CostSharing.Core.Models;
using CostSharing.Core.Services;
using SQLite;

namespace CostSharingApp.Services;

/// <summary>
/// Service for resolving sync conflicts when multiple users edit simultaneously.
/// </summary>
public class ConflictResolutionService : IConflictResolver
{
    private readonly SQLiteAsyncConnection database;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictResolutionService"/> class.
    /// </summary>
    public ConflictResolutionService(SQLiteAsyncConnection database)
    {
        this.database = database;
    }

    /// <inheritdoc/>
    public async Task<List<Expense>> MergeExpensesAsync(List<Expense> localExpenses, List<Expense> remoteExpenses)
    {
        // Append-only merge: Include all expenses from both lists
        // Never delete expenses created by other users
        var mergedExpenses = new Dictionary<Guid, Expense>();

        // Add all local expenses
        foreach (var expense in localExpenses)
        {
            mergedExpenses[expense.Id] = expense;
        }

        // Add remote expenses, updating if more recent
        foreach (var remoteExpense in remoteExpenses)
        {
            if (mergedExpenses.TryGetValue(remoteExpense.Id, out var localExpense))
            {
                // Expense exists locally - compare timestamps
                if (remoteExpense.ModifiedTimestamp > localExpense.ModifiedTimestamp)
                {
                    // Remote is newer, use remote version
                    mergedExpenses[remoteExpense.Id] = remoteExpense;
                }
                // else: Local is newer or equal, keep local
            }
            else
            {
                // New expense from remote, add it
                mergedExpenses[remoteExpense.Id] = remoteExpense;
            }
        }

        return await Task.FromResult(mergedExpenses.Values.ToList());
    }

    /// <inheritdoc/>
    public async Task<Group> MergeGroupMetadataAsync(Group localGroup, Group remoteGroup)
    {
        // Last-Write-Wins strategy: Use the group with more recent UpdatedAt timestamp
        if (remoteGroup.UpdatedAt > localGroup.UpdatedAt)
        {
            // Remote is newer
            return await Task.FromResult(remoteGroup);
        }
        else if (localGroup.UpdatedAt > remoteGroup.UpdatedAt)
        {
            // Local is newer
            return await Task.FromResult(localGroup);
        }
        else
        {
            // Equal timestamps - use remote as tiebreaker
            return await Task.FromResult(remoteGroup);
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> DetectConflictsAsync(Guid groupId)
    {
        var conflicts = new List<string>();

        // Check for expenses with conflict status
        var conflictExpenses = await this.database.Table<Expense>()
            .Where(e => e.GroupId == groupId && e.SyncStatus == SyncStatus.Conflict)
            .ToListAsync();

        foreach (var expense in conflictExpenses)
        {
            conflicts.Add($"Expense '{expense.Description}' has a sync conflict (ID: {expense.Id})");
        }

        // Check SyncMetadata for conflicts
        var syncMetadata = await this.database.Table<SyncMetadata>()
            .Where(s => s.GroupId == groupId && s.HasConflict)
            .ToListAsync();

        foreach (var metadata in syncMetadata)
        {
            if (!string.IsNullOrEmpty(metadata.ConflictDetails))
            {
                conflicts.Add($"{metadata.EntityType} conflict: {metadata.ConflictDetails}");
            }
            else
            {
                conflicts.Add($"{metadata.EntityType} has an unresolved conflict");
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Resolves conflicts by accepting remote version for all conflicting items.
    /// </summary>
    /// <param name="groupId">Group to resolve conflicts for.</param>
    public async Task ResolveConflictsByAcceptingRemoteAsync(Guid groupId)
    {
        // Mark all conflicting expenses as synced
        var conflictExpenses = await this.database.Table<Expense>()
            .Where(e => e.GroupId == groupId && e.SyncStatus == SyncStatus.Conflict)
            .ToListAsync();

        foreach (var expense in conflictExpenses)
        {
            expense.SyncStatus = SyncStatus.Synced;
            await this.database.UpdateAsync(expense);
        }

        // Clear conflict flags in SyncMetadata
        var syncMetadata = await this.database.Table<SyncMetadata>()
            .Where(s => s.GroupId == groupId && s.HasConflict)
            .ToListAsync();

        foreach (var metadata in syncMetadata)
        {
            metadata.HasConflict = false;
            metadata.ConflictDetails = null;
            await this.database.UpdateAsync(metadata);
        }
    }
}
