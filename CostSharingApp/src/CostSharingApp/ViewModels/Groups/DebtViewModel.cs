
namespace CostSharingApp.ViewModels.Groups;

/// <summary>
/// View model for displaying debt information with user names.
/// </summary>
public class DebtViewModel
{
    /// <summary>
    /// Gets or sets the debt ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the debtor user ID.
    /// </summary>
    public Guid DebtorId { get; set; }

    /// <summary>
    /// Gets or sets the debtor name for display.
    /// </summary>
    public string DebtorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creditor user ID.
    /// </summary>
    public Guid CreditorId { get; set; }

    /// <summary>
    /// Gets or sets the creditor name for display.
    /// </summary>
    public string CreditorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the debt amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the group ID.
    /// </summary>
    public Guid GroupId { get; set; }
}
