namespace CostSharing.Core.Models;

/// <summary>
/// Status of a settlement.
/// </summary>
public enum SettlementStatus
{
    /// <summary>
    /// Settlement recorded but not yet confirmed.
    /// </summary>
    Pending,

    /// <summary>
    /// Settlement confirmed by both parties.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Settlement cancelled.
    /// </summary>
    Cancelled
}

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
    /// Gets or sets when the settlement was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the settlement was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets when payment was made.
    /// </summary>
    public DateTime SettlementDate { get; set; }

    /// <summary>
    /// Gets or sets when the settlement was confirmed.
    /// </summary>
    public DateTime? ConfirmedDate { get; set; }

    /// <summary>
    /// Gets or sets who recorded the settlement.
    /// </summary>
    public Guid RecordedBy { get; set; }

    /// <summary>
    /// Gets or sets the status of the settlement.
    /// </summary>
    public SettlementStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when payment was made (legacy property for backward compatibility).
    /// </summary>
    public DateTime SettledAt
    {
        get => this.SettlementDate;
        set => this.SettlementDate = value;
    }
}
