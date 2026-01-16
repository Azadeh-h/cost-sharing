// <copyright file="DatabaseMigrations.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using CostSharing.Core.Models;
using CostSharing.Core.Services;

namespace CostSharingApp.Services;

/// <summary>
/// Database migration utilities for updating existing data.
/// </summary>
public static class DatabaseMigrations
{
    /// <summary>
    /// Updates Sara Chen's email address to behrangDarya@gmail.com.
    /// </summary>
    /// <param name="cacheService">Cache service for database access.</param>
    /// <param name="loggingService">Logging service.</param>
    /// <returns>Task representing the async operation.</returns>
    public static async Task UpdateSaraChenEmailAsync(
        ICacheService cacheService,
        ILoggingService loggingService)
    {
        try
        {
            // Get all users
            var allUsers = await cacheService.GetAllAsync<User>();

            // Find Sara Chen (try both "Sara" and "Sarah" spellings)
            var saraUser = allUsers.FirstOrDefault(u =>
                u.Name.Equals("Sarah Chen", StringComparison.OrdinalIgnoreCase));

            if (saraUser == null)
            {
                return;
            }

            saraUser.Email = "behrangDarya@gmail.com";
            await cacheService.SaveAsync(saraUser);
        }
        catch (Exception ex)
        {
            loggingService.LogError("Failed to update Sara Chen's email", ex);
            throw;
        }
    }
}
