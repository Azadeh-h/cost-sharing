using CostSharing.Core.Models;
using CostSharing.Core.Services;

namespace CostSharingApp.Services;

/// <summary>
/// Manages group operations including CRUD via SQLite.
/// </summary>
public class GroupService : IGroupService
{
    private readonly ICacheService cacheService;
    private readonly IAuthService authService;
    private readonly ILoggingService loggingService;
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupService"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service.</param>
    /// <param name="authService">Auth service.</param>
    /// <param name="loggingService">Logging service.</param>
    /// <param name="serviceProvider">Service provider for lazy resolution.</param>
    public GroupService(
        ICacheService cacheService,
        IAuthService authService,
        ILoggingService loggingService,
        IServiceProvider serviceProvider)
    {
        this.cacheService = cacheService;
        this.authService = authService;
        this.loggingService = loggingService;
        this.serviceProvider = serviceProvider;
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

            // Save group first
            await this.cacheService.SaveAsync(group);

            // Check if user is already a member (shouldn't happen for new group, but safety check)
            var existingMembers = await this.GetGroupMembersAsync(group.Id);
            if (!existingMembers.Any(m => m.UserId == currentUser.Id))
            {
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
                await this.cacheService.SaveAsync(adminMember);
            }

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

            // Save to SQLite database
            await this.cacheService.SaveAsync(group);

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

            // Get user email for Drive permission removal
            var userToRemove = await this.authService.GetUserByIdAsync(userId);

            // Delete member from local database
            await this.cacheService.DeleteAsync(memberToRemove);

            // Unshare Drive folder with the removed member
            await this.UnshareFolderWithMemberAsync(groupId, userToRemove?.Email, currentUser.Id);

            this.loggingService.LogInfo($"User {userId} removed from group {groupId}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to remove member", ex);
            return false;
        }
    }

    /// <summary>
    /// Adds a member to a group by email. Creates user if not exists.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="email">Email of member to add.</param>
    /// <param name="name">Name of member to add.</param>
    /// <returns>True if successful, error message if failed.</returns>
    public async Task<(bool Success, string? ErrorMessage)> AddMemberByEmailAsync(Guid groupId, string email, string name)
    {
        try
        {
            var currentUser = this.authService.GetCurrentUser();
            if (currentUser == null)
            {
                return (false, "You must be logged in to add members");
            }

            // Check if user is a member of the group
            var members = await this.GetGroupMembersAsync(groupId);
            var currentUserMembership = members.FirstOrDefault(m => m.UserId == currentUser.Id);
            if (currentUserMembership == null)
            {
                return (false, "You are not a member of this group");
            }

            // Find or create user by email
            var allUsers = await this.authService.GetAllUsersAsync();
            var existingUser = allUsers.FirstOrDefault(u => 
                u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

            User userToAdd;
            if (existingUser != null)
            {
                userToAdd = existingUser;
            }
            else
            {
                // Create a new user for this email
                userToAdd = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email.Trim().ToLowerInvariant(),
                    Name = name.Trim(),
                    PasswordHash = string.Empty, // No password needed for invited users
                    CreatedAt = DateTime.UtcNow,
                    IsEmailVerified = false,
                };
                await this.cacheService.SaveAsync(userToAdd);
                this.loggingService.LogInfo($"Created user for invited member: {email}");
            }

            // Check if already a member
            if (members.Any(m => m.UserId == userToAdd.Id))
            {
                return (false, $"{name} is already a member of this group");
            }

            // Add as member
            var newMember = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = userToAdd.Id,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow,
                AddedBy = currentUser.Id,
            };
            await this.cacheService.SaveAsync(newMember);

            this.loggingService.LogInfo($"User {userToAdd.Email} added to group {groupId}");
            return (true, null);
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to add member", ex);
            return (false, $"Failed to add member: {ex.Message}");
        }
    }

    /// <summary>
    /// Unshares the group's Google Drive folder with a removed member.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="memberEmail">The email of the member to remove access for.</param>
    /// <param name="currentUserId">The current user ID (must be group admin/folder owner).</param>
    /// <returns>Task for async operation.</returns>
    private async Task UnshareFolderWithMemberAsync(Guid groupId, string? memberEmail, Guid currentUserId)
    {
        try
        {
            if (string.IsNullOrEmpty(memberEmail) || memberEmail.EndsWith("@device.local"))
            {
                this.loggingService.LogInfo("Skipping Drive unshare - no valid email for removed member");
                return;
            }

            // Get the group to find its Drive folder ID
            var group = await this.GetGroupAsync(groupId);
            if (group == null || string.IsNullOrEmpty(group.DriveFolderId))
            {
                this.loggingService.LogInfo($"No Drive folder associated with group {groupId}, skipping unshare");
                return;
            }

            // Resolve DriveSyncService lazily to avoid circular dependency
            var driveSyncService = this.serviceProvider.GetService<IDriveSyncService>();
            if (driveSyncService == null)
            {
                this.loggingService.LogWarning("DriveSyncService not available, skipping folder unshare");
                return;
            }

            var success = await driveSyncService.RemoveFolderPermissionAsync(
                group.DriveFolderId,
                memberEmail,
                currentUserId);

            if (success)
            {
                this.loggingService.LogInfo($"Successfully unshared Drive folder with {memberEmail}");
            }
            else
            {
                this.loggingService.LogWarning($"Failed to unshare Drive folder with {memberEmail}");
            }
        }
        catch (Exception ex)
        {
            // Don't fail the member removal if Drive unsharing fails
            this.loggingService.LogError($"Error unsharing Drive folder with member: {ex.Message}", ex);
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

    /// <summary>
    /// Adds a member to a group by email.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="email">Email of member to add.</param>
    /// <param name="name">Name of member to add.</param>
    /// <returns>Success flag and optional error message.</returns>
    Task<(bool Success, string? ErrorMessage)> AddMemberByEmailAsync(Guid groupId, string email, string name);
}
