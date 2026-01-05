using CostSharing.Core.Models;

namespace CostSharingApp.Services;

/// <summary>
/// Service for managing settlement transactions between users.
/// </summary>
public interface ISettlementService
{
    /// <summary>
    /// Records a new settlement transaction.
    /// </summary>
    /// <param name="settlement">The settlement to record.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RecordSettlementAsync(Settlement settlement);

    /// <summary>
    /// Gets all settlements for a specific group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <returns>List of settlements.</returns>
    Task<List<Settlement>> GetGroupSettlementsAsync(Guid groupId);

    /// <summary>
    /// Gets settlements for a specific user in a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of settlements.</returns>
    Task<List<Settlement>> GetUserSettlementsAsync(Guid groupId, Guid userId);

    /// <summary>
    /// Confirms a settlement transaction.
    /// </summary>
    /// <param name="settlementId">The settlement ID.</param>
    /// <param name="confirmedByUserId">The user confirming the settlement.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ConfirmSettlementAsync(Guid settlementId, Guid confirmedByUserId);

    /// <summary>
    /// Cancels a settlement transaction.
    /// </summary>
    /// <param name="settlementId">The settlement ID.</param>
    /// <returns>Task representing the async operation.</returns>
    Task CancelSettlementAsync(Guid settlementId);

    /// <summary>
    /// Deletes a settlement from local cache and Google Drive.
    /// </summary>
    /// <param name="settlementId">The settlement ID to delete.</param>
    /// <returns>Task representing the async operation.</returns>
    Task DeleteSettlementAsync(Guid settlementId);
}
