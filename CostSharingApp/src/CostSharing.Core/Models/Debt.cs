namespace CostSharing.Core.Models;

/// <summary>
/// Represents a simplified debt relationship between two members.
/// </summary>
public class Debt
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the group reference.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the user who owes money.
    /// </summary>
    public Guid DebtorId { get; set; }

    /// <summary>
    /// Gets or sets the user owed money.
    /// </summary>
    public Guid CreditorId { get; set; }

    /// <summary>
    /// Gets or sets the amount owed in AUD.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets when debt was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}
