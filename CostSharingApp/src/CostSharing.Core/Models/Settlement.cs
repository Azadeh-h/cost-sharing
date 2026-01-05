namespace CostSharing.Core.Models;

/// <summary>
/// Represents a payment made to settle a debt between members.
/// </summary>
public class Settlement
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
    /// Gets or sets the debt being settled.
    /// </summary>
    public Guid DebtId { get; set; }

    /// <summary>
    /// Gets or sets who paid.
    /// </summary>
    public Guid PaidBy { get; set; }

    /// <summary>
    /// Gets or sets who received payment.
    /// </summary>
    public Guid PaidTo { get; set; }

    /// <summary>
    /// Gets or sets the payment amount in AUD.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets when payment was made.
    /// </summary>
    public DateTime SettledAt { get; set; }

    /// <summary>
    /// Gets or sets who recorded the settlement.
    /// </summary>
    public Guid RecordedBy { get; set; }
}
