using SQLite;

namespace CostSharing.Core.Models;

/// <summary>
/// Represents one member's share of an expense (even or custom percentage).
/// </summary>
public class ExpenseSplit
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [PrimaryKey]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the expense reference.
    /// </summary>
    public Guid ExpenseId { get; set; }

    /// <summary>
    /// Gets or sets the user who owes this split.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the split percentage (0-100).
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Gets or sets the calculated amount owed.
    /// </summary>
    public decimal Amount { get; set; }
}
