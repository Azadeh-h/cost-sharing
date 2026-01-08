
using CostSharing.Core.Models;

namespace CostSharingApp.Models.GoogleSync;
/// <summary>
/// Data transfer object for syncing group data with Google Drive.
/// </summary>
public class GroupSyncDto
{
    /// <summary>
    /// Gets or sets the group information.
    /// </summary>
    public Group? Group { get; set; }

    /// <summary>
    /// Gets or sets the list of group members.
    /// </summary>
    public List<GroupMember> Members { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of expenses.
    /// </summary>
    public List<Expense> Expenses { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of expense splits.
    /// </summary>
    public List<ExpenseSplit> ExpenseSplits { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of settlements.
    /// </summary>
    public List<Settlement> Settlements { get; set; } = new();

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the version number for conflict resolution.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the email of the user who last modified this data.
    /// </summary>
    public string? LastModifiedBy { get; set; }
}
