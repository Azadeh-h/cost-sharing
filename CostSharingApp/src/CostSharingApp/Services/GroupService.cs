using CostSharing.Core.Models;

namespace CostSharingApp.Services;

/// <summary>
/// Manages group operations including CRUD and Google Drive synchronization.
/// </summary>
public class GroupService : IGroupService
{
    private readonly IDriveService driveService;
    private readonly ICacheService cacheService;
    private readonly IAuthService authService;
    private readonly ILoggingService loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupService"/> class.
    /// </summary>
    /// <param name="driveService">Drive service.</param>
    /// <param name="cacheService">Cache service.</param>
    /// <param name="authService">Auth service.</param>
    /// <param name="loggingService">Logging service.</param>
    public GroupService(
        IDriveService driveService,
        ICacheService cacheService,
        IAuthService authService,
        ILoggingService loggingService)
    {
        this.driveService = driveService;
        this.cacheService = cacheService;
        this.authService = authService;
        this.loggingService = loggingService;
    }

    /// <summary>
    /// Creates a new group with the current user as admin.
    /// </summary>
    /// <param name="name">Group name (1-100 chars).</param>
    /// <returns>Created group or null if failed.</returns>
    public async Task<Group?> CreateGroupAsync(string name)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                // Auto-create a default user if none exists
                this.loggingService.LogInfo("No user found, creating default user");
                var deviceId = Preferences.Get("DeviceUserId", Guid.NewGuid().ToString());
                Preferences.Set("DeviceUserId", deviceId);
                
                var email = $"{deviceId}@device.local";
                var password = "default123";
                
                var loginResult = await this.authService.LoginAsync(email, password);
                if (!loginResult)
                {
                    await this.authService.RegisterAsync(email, password, "Device User");
                }
                
                currentUser = this.authService.GetCurrentUser();
                if (currentUser == null)
                {
                    this.loggingService.LogWarning("Failed to create/load user");
                    return null;
                }
            }

            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            {
                this.loggingService.LogWarning($"Invalid group name: {name}");
                return null;
            }

            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                CreatorId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Currency = "AUD"
            };

            // Add creator as admin member
            var adminMember = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                UserId = currentUser.Id,
                Role = GroupRole.Admin,
                JoinedAt = DateTime.UtcNow,
                AddedBy = currentUser.Id
            };

            // Save to local cache
            await this.cacheService.SaveAsync(group);
            await this.cacheService.SaveAsync(adminMember);

            // Sync to Google Drive
            await this.driveService.SaveFileAsync($"group_{group.Id}.json", group);
            await this.driveService.SaveFileAsync($"group_{group.Id}_members.json", new List<GroupMember> { adminMember });

            this.loggingService.LogInfo($"Group created: {group.Name} ({group.Id})");
            return group;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Group creation failed", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets all groups where the current user is a member.
    /// </summary>
    /// <returns>List of groups.</returns>
    public async Task<List<Group>> GetUserGroupsAsync()
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return new List<Group>();
            }

            // Get all group memberships for current user
            var allMembers = await this.cacheService.GetAllAsync<GroupMember>();
            var userMemberships = allMembers.Where(m => m.UserId == currentUser.Id).ToList();

            // Get corresponding groups
            var allGroups = await this.cacheService.GetAllAsync<Group>();
            var userGroups = allGroups.Where(g => userMemberships.Any(m => m.GroupId == g.Id)).ToList();

            return userGroups;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to get user groups", ex);
            return new List<Group>();
        }
    }

    /// <summary>
    /// Gets a specific group by ID.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Group or null if not found.</returns>
    public async Task<Group?> GetGroupAsync(Guid groupId)
    {
        try
        {
            return await this.cacheService.GetAsync<Group>(groupId);
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to get group {groupId}", ex);
            return null;
        }
    }

    /// <summary>
    /// Updates group information.
    /// </summary>
    /// <param name="group">Updated group.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> UpdateGroupAsync(Group group)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return false;
            }

            // Check if user is admin
            var members = await this.GetGroupMembersAsync(group.Id);
            var userMembership = members.FirstOrDefault(m => m.UserId == currentUser.Id);
            if (userMembership == null || userMembership.Role != GroupRole.Admin)
            {
                this.loggingService.LogWarning($"User {currentUser.Id} not authorized to update group {group.Id}");
                return false;
            }

            group.UpdatedAt = DateTime.UtcNow;

            // Save to cache and Drive
            await this.cacheService.SaveAsync(group);
            await this.driveService.SaveFileAsync($"group_{group.Id}.json", group);

            this.loggingService.LogInfo($"Group updated: {group.Id}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Group update failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Deletes a group (admin only).
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> DeleteGroupAsync(Guid groupId)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return false;
            }

            var group = await this.GetGroupAsync(groupId);
            if (group == null)
            {
                return false;
            }

            // Check if user is admin
            var members = await this.GetGroupMembersAsync(groupId);
            var userMembership = members.FirstOrDefault(m => m.UserId == currentUser.Id);
            if (userMembership == null || userMembership.Role != GroupRole.Admin)
            {
                this.loggingService.LogWarning($"User {currentUser.Id} not authorized to delete group {groupId}");
                return false;
            }

            // Delete from cache
            await this.cacheService.DeleteAsync(group);
            foreach (var member in members)
            {
                await this.cacheService.DeleteAsync(member);
            }

            this.loggingService.LogInfo($"Group deleted: {groupId}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Group deletion failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets all members of a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>List of group members.</returns>
    public async Task<List<GroupMember>> GetGroupMembersAsync(Guid groupId)
    {
        try
        {
            var allMembers = await this.cacheService.GetAllAsync<GroupMember>();
            return allMembers.Where(m => m.GroupId == groupId).ToList();
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to get members for group {groupId}", ex);
            return new List<GroupMember>();
        }
    }

    /// <summary>
    /// Removes a member from a group (admin only).
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="userId">User ID to remove.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid userId)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return false;
            }

            // Check if current user is admin
            var members = await this.GetGroupMembersAsync(groupId);
            var currentUserMembership = members.FirstOrDefault(m => m.UserId == currentUser.Id);
            if (currentUserMembership == null || currentUserMembership.Role != GroupRole.Admin)
            {
                this.loggingService.LogWarning($"User {currentUser.Id} not authorized to remove members from group {groupId}");
                return false;
            }

            // Find member to remove
            var memberToRemove = members.FirstOrDefault(m => m.UserId == userId);
            if (memberToRemove == null)
            {
                return false;
            }

            // Delete member
            await this.cacheService.DeleteAsync(memberToRemove);

            this.loggingService.LogInfo($"User {userId} removed from group {groupId}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to remove member", ex);
            return false;
        }
    }
}

/// <summary>
/// Interface for group service.
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Creates a new group.
    /// </summary>
    /// <param name="name">Group name.</param>
    /// <returns>Created group or null.</returns>
    Task<Group?> CreateGroupAsync(string name);

    /// <summary>
    /// Gets all groups for current user.
    /// </summary>
    /// <returns>List of groups.</returns>
    Task<List<Group>> GetUserGroupsAsync();

    /// <summary>
    /// Gets a specific group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Group or null.</returns>
    Task<Group?> GetGroupAsync(Guid groupId);

    /// <summary>
    /// Updates a group.
    /// </summary>
    /// <param name="group">Group to update.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateGroupAsync(Group group);

    /// <summary>
    /// Deletes a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteGroupAsync(Guid groupId);

    /// <summary>
    /// Gets all members of a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>List of members.</returns>
    Task<List<GroupMember>> GetGroupMembersAsync(Guid groupId);

    /// <summary>
    /// Removes a member from a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="userId">User ID to remove.</param>
    /// <returns>True if successful.</returns>
    Task<bool> RemoveMemberAsync(Guid groupId, Guid userId);
}
