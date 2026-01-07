using SQLite;

namespace CostSharing.Core.Models;

/// <summary>
/// Split type for expenses.
/// </summary>
public enum SplitType
{
    /// <summary>
    /// Split evenly among all participants.
    /// </summary>
    Even,

    /// <summary>
    /// Split by custom percentages.
    /// </summary>
    Custom,
}

/// <summary>
/// Represents an expense within a group, paid by one member for multiple participants.
/// </summary>
public class Expense
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [PrimaryKey]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the group reference.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the description (1-200 chars).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount in AUD.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets who paid the expense.
    /// </summary>
    public Guid PaidBy { get; set; }

    /// <summary>
    /// Gets or sets the split type (even or custom).
    /// </summary>
    public SplitType SplitType { get; set; }

    /// <summary>
    /// Gets or sets when expense occurred.
    /// </summary>
    public DateTime ExpenseDate { get; set; }

    /// <summary>
    /// Gets or sets when expense was recorded.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets who recorded the expense.
    /// </summary>
    public Guid CreatedBy { get; set; }
}
