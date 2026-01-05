using System.Text.Json;
using CostSharing.Core.Models;

namespace CostSharingApp.Services;

/// <summary>
/// Service for managing settlement transactions with Google Drive persistence.
/// </summary>
public class SettlementService : ISettlementService
{
    private readonly ICacheService cacheService;
    private readonly IDriveService driveService;
    private readonly ILoggingService loggingService;
    private const string SettlementsCacheKey = "settlements";
    private const string SettlementsFolderName = "settlements";

    /// <summary>
    /// Initializes a new instance of the <see cref="SettlementService"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service for local storage.</param>
    /// <param name="driveService">Drive service for cloud sync.</param>
    /// <param name="loggingService">Logging service.</param>
    public SettlementService(
        ICacheService cacheService,
        IDriveService driveService,
        ILoggingService loggingService)
    {
        this.cacheService = cacheService;
        this.driveService = driveService;
        this.loggingService = loggingService;
    }

    /// <inheritdoc/>
    public async Task RecordSettlementAsync(Settlement settlement)
    {
        try
        {
            // Get existing settlements
            var settlements = await this.GetAllSettlementsAsync();

            // Add new settlement
            settlement.CreatedAt = DateTime.UtcNow;
            settlement.UpdatedAt = DateTime.UtcNow;
            settlements.Add(settlement);

            // Save to cache
            await this.cacheService.SaveAsync(SettlementsCacheKey, settlements);

            // Sync to Google Drive
            var json = JsonSerializer.Serialize(settlement);
            var fileName = $"settlement_{settlement.Id}.json";
            await this.driveService.UploadFileAsync(fileName, json, SettlementsFolderName);

            this.loggingService.LogInfo($"Settlement recorded: {settlement.Id}");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to record settlement: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Settlement>> GetGroupSettlementsAsync(Guid groupId)
    {
        var allSettlements = await this.GetAllSettlementsAsync();
        return allSettlements
            .Where(s => s.GroupId == groupId && s.Status != SettlementStatus.Cancelled)
            .OrderByDescending(s => s.SettlementDate)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<List<Settlement>> GetUserSettlementsAsync(Guid groupId, Guid userId)
    {
        var allSettlements = await this.GetAllSettlementsAsync();
        return allSettlements
            .Where(s => s.GroupId == groupId &&
                       (s.PaidBy == userId || s.PaidTo == userId) &&
                       s.Status != SettlementStatus.Cancelled)
            .OrderByDescending(s => s.SettlementDate)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task ConfirmSettlementAsync(Guid settlementId, Guid confirmedByUserId)
    {
        try
        {
            var settlements = await this.GetAllSettlementsAsync();
            var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);

            if (settlement == null)
            {
                throw new InvalidOperationException($"Settlement {settlementId} not found");
            }

            settlement.Status = SettlementStatus.Confirmed;
            settlement.ConfirmedByUserId = confirmedByUserId;
            settlement.ConfirmedDate = DateTime.UtcNow;
            settlement.UpdatedAt = DateTime.UtcNow;

            // Save to cache
            await this.cacheService.SaveAsync(SettlementsCacheKey, settlements);

            // Update in Google Drive
            var json = JsonSerializer.Serialize(settlement);
            var fileName = $"settlement_{settlement.Id}.json";
            await this.driveService.UploadFileAsync(fileName, json, SettlementsFolderName);

            this.loggingService.LogInfo($"Settlement confirmed: {settlementId}");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to confirm settlement: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task CancelSettlementAsync(Guid settlementId)
    {
        try
        {
            var settlements = await this.GetAllSettlementsAsync();
            var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);

            if (settlement == null)
            {
                throw new InvalidOperationException($"Settlement {settlementId} not found");
            }

            settlement.Status = SettlementStatus.Cancelled;
            settlement.UpdatedAt = DateTime.UtcNow;

            // Save to cache
            await this.cacheService.SaveAsync(SettlementsCacheKey, settlements);

            // Update in Google Drive
            var json = JsonSerializer.Serialize(settlement);
            var fileName = $"settlement_{settlement.Id}.json";
            await this.driveService.UploadFileAsync(fileName, json, SettlementsFolderName);

            this.loggingService.LogInfo($"Settlement cancelled: {settlementId}");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to cancel settlement: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteSettlementAsync(Guid settlementId)
    {
        try
        {
            var settlements = await this.GetAllSettlementsAsync();
            var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);

            if (settlement == null)
            {
                return; // Already deleted
            }

            settlements.Remove(settlement);

            // Save to cache
            await this.cacheService.SaveAsync(SettlementsCacheKey, settlements);

            // Delete from Google Drive
            var fileName = $"settlement_{settlementId}.json";
            await this.driveService.DeleteFileAsync(fileName, SettlementsFolderName);

            this.loggingService.LogInfo($"Settlement deleted: {settlementId}");
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to delete settlement: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets all settlements from cache.
    /// </summary>
    /// <returns>List of all settlements.</returns>
    private async Task<List<Settlement>> GetAllSettlementsAsync()
    {
        var settlements = await this.cacheService.GetAsync<List<Settlement>>(SettlementsCacheKey);
        return settlements ?? new List<Settlement>();
    }
}
