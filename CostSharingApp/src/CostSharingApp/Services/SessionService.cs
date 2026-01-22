// <copyright file="SessionService.cs" company="CostSharingApp">
// Copyright (c) CostSharingApp. All rights reserved.
// </copyright>

namespace CostSharingApp.Services;

/// <summary>
/// Manages user session persistence using SecureStorage.
/// Sessions never expire - user stays signed in until explicit sign out.
/// </summary>
public class SessionService : ISessionService
{
    private const string SessionUserIdKey = "session_user_id";

    /// <inheritdoc/>
    public async Task SaveSessionAsync(Guid userId)
    {
        await SecureStorage.Default.SetAsync(SessionUserIdKey, userId.ToString());
    }

    /// <inheritdoc/>
    public async Task<Guid?> GetSessionAsync()
    {
        try
        {
            var userIdString = await SecureStorage.Default.GetAsync(SessionUserIdKey);
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public Task ClearSessionAsync()
    {
        SecureStorage.Default.Remove(SessionUserIdKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> HasSessionAsync()
    {
        var userId = await this.GetSessionAsync();
        return userId.HasValue;
    }
}
