
using System.Security.Cryptography;
using System.Text;
using CostSharing.Core.Interfaces;
using CostSharing.Core.Models;

namespace CostSharingApp.Services;
/// <summary>
/// Provides authentication services including email/password and magic link.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ICacheService cacheService;
    private readonly ILoggingService loggingService;
    private readonly IServiceProvider serviceProvider;
    private CostSharing.Core.Models.User? currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service for credential storage.</param>
    /// <param name="loggingService">Logging service.</param>
    /// <param name="serviceProvider">Service provider for lazy dependency resolution.</param>
    public AuthService(ICacheService cacheService, ILoggingService loggingService, IServiceProvider serviceProvider)
    {
        this.cacheService = cacheService;
        this.loggingService = loggingService;
        this.serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Registers new user with email and password.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <param name="password">User password.</param>
    /// <param name="name">User display name.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> RegisterAsync(string email, string password, string name, string? phone = null)
    {
        try
        {
            // Normalize email for case-insensitive comparison
            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Check if user already exists
            var existingUsers = await this.cacheService.GetAllAsync<CostSharing.Core.Models.User>();
            if (existingUsers.Any(u => u.Email.Trim().ToLowerInvariant() == normalizedEmail))
            {
                this.loggingService.LogWarning($"Registration failed: Email {email} already exists");
                return false;
            }

            var user = new CostSharing.Core.Models.User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = this.HashPassword(password),
                Name = name,
                Phone = phone,
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = false
            };

            await this.cacheService.SaveAsync(user);
            this.currentUser = user;
            this.loggingService.LogInfo($"User registered: {normalizedEmail}");

            // Link any pending invitations for this email
            await this.LinkPendingInvitationsForUserAsync(user.Id, normalizedEmail);

            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Registration failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Authenticates user with email and password.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <param name="password">User password.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            // Normalize email for case-insensitive comparison
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var users = await this.cacheService.GetAllAsync<CostSharing.Core.Models.User>();
            var user = users.FirstOrDefault(u => u.Email.Trim().ToLowerInvariant() == normalizedEmail);

            if (user == null)
            {
                this.loggingService.LogWarning($"Login failed: User {email} not found");
                return false;
            }

            if (!this.VerifyPassword(password, user.PasswordHash))
            {
                this.loggingService.LogWarning($"Login failed: Invalid password for {email}");
                return false;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await this.cacheService.SaveAsync(user);

            this.currentUser = user;
            this.loggingService.LogInfo($"User logged in: {email}");

            // Link any pending invitations for this email (in case new invitations arrived since last login)
            await this.LinkPendingInvitationsForUserAsync(user.Id, user.Email);

            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Login failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Generates magic link token for passwordless authentication.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <returns>Magic link token or null if failed.</returns>
    public async Task<string?> GenerateMagicLinkAsync(string email)
    {
        try
        {
            var users = await this.cacheService.GetAllAsync<CostSharing.Core.Models.User>();
            var user = users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return null;
            }

            // Generate secure random token
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            this.loggingService.LogInfo($"Magic link generated for {email}");
            return token;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Magic link generation failed", ex);
            return null;
        }
    }

    /// <summary>
    /// Signs out current user.
    /// </summary>
    public void SignOut()
    {
        this.loggingService.LogInfo($"User signed out: {this.currentUser?.Email}");
        this.currentUser = null;
    }

    /// <summary>
    /// Gets currently authenticated user.
    /// </summary>
    /// <returns>Current user or null.</returns>
    public CostSharing.Core.Models.User? GetCurrentUser()
    {
        return this.currentUser;
    }

    /// <summary>
    /// Checks if user is authenticated.
    /// </summary>
    /// <returns>True if authenticated.</returns>
    public bool IsAuthenticated()
    {
        return this.currentUser != null;
    }

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="userId">User ID to lookup.</param>
    /// <returns>User or null if not found.</returns>
    public async Task<CostSharing.Core.Models.User?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var user = await this.cacheService.GetAsync<CostSharing.Core.Models.User>(userId);
            
            // Fallback to GetAllAsync if not found by ID (in case of caching issues)
            if (user == null)
            {
                var allUsers = await this.cacheService.GetAllAsync<CostSharing.Core.Models.User>();
                user = allUsers.FirstOrDefault(u => u.Id == userId);
            }
            
            return user;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Failed to get user {userId}", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets all users in the system.
    /// </summary>
    /// <returns>List of all users.</returns>
    public async Task<List<CostSharing.Core.Models.User>> GetAllUsersAsync()
    {
        try
        {
            return await this.cacheService.GetAllAsync<CostSharing.Core.Models.User>();
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to get all users", ex);
            return new List<CostSharing.Core.Models.User>();
        }
    }

    /// <summary>
    /// Removes duplicate users with the same name who are not members of any group.
    /// </summary>
    /// <returns>Number of duplicate users removed.</returns>
    public async Task<int> RemoveDuplicateUnusedUsersAsync()
    {
        try
        {
            var allUsers = await this.cacheService.GetAllAsync<CostSharing.Core.Models.User>();
            var allMembers = await this.cacheService.GetAllAsync<GroupMember>();

            // Get user IDs that are members of groups
            var usersInGroups = new HashSet<Guid>(allMembers.Select(m => m.UserId));

            // Find duplicate names
            var duplicateNameGroups = allUsers
                .GroupBy(u => u.Name)
                .Where(g => g.Count() > 1);

            int removedCount = 0;
            foreach (var duplicateGroup in duplicateNameGroups)
            {
                // Among duplicates, keep only those who are in groups
                var usersToRemove = duplicateGroup
                    .Where(u => !usersInGroups.Contains(u.Id))
                    .ToList();

                foreach (var user in usersToRemove)
                {
                    await this.cacheService.DeleteAsync(user);
                    this.loggingService.LogInfo($"Removed duplicate unused user: {user.Name} ({user.Email})");
                    removedCount++;
                }
            }

            return removedCount;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("Failed to remove duplicate unused users", ex);
            return 0;
        }
    }

    /// <summary>
    /// Updates user profile information.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="name">New name.</param>
    /// <param name="email">New email.</param>
    /// <param name="phone">New phone.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> UpdateUserAsync(Guid userId, string name, string email, string? phone = null)
    {
        try
        {
            var user = await this.GetUserByIdAsync(userId);
            if (user == null)
            {
                this.loggingService.LogWarning($"Update failed: User {userId} not found");
                return false;
            }

            // Check if new email is already used by another user
            if (user.Email != email)
            {
                var existingUsers = await this.cacheService.GetAllAsync<CostSharing.Core.Models.User>();
                if (existingUsers.Any(u => u.Email == email && u.Id != userId))
                {
                    this.loggingService.LogWarning($"Update failed: Email {email} already exists");
                    return false;
                }
            }

            user.Name = name;
            user.Email = email;
            user.Phone = phone;

            await this.cacheService.SaveAsync(user);

            // Update current user if it's the same
            if (this.currentUser?.Id == userId)
            {
                this.currentUser = user;
            }

            this.loggingService.LogInfo($"User profile updated: {email}");
            return true;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError("User update failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Links pending invitations for a user (called after login/registration).
    /// Uses IServiceProvider to resolve IInvitationLinkingService to avoid circular dependency.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="email">User email.</param>
    private async Task LinkPendingInvitationsForUserAsync(Guid userId, string email)
    {
        try
        {
            var invitationLinkingService = this.serviceProvider.GetService<IInvitationLinkingService>();
            if (invitationLinkingService != null)
            {
                var linkedCount = await invitationLinkingService.LinkPendingInvitationsAsync(userId, email);
                if (linkedCount > 0)
                {
                    this.loggingService.LogInfo($"Linked {linkedCount} pending invitation(s) for user {email}");
                }
            }
        }
        catch (Exception ex)
        {
            // Non-blocking - log and continue
            this.loggingService.LogError($"Failed to link pending invitations for {email}", ex);
        }
    }

    /// <summary>
    /// Hashes password using SHA256.
    /// </summary>
    /// <param name="password">Plain password.</param>
    /// <returns>Hashed password.</returns>
    private string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Verifies password against hash.
    /// </summary>
    /// <param name="password">Plain password.</param>
    /// <param name="hash">Stored hash.</param>
    /// <returns>True if match.</returns>
    private bool VerifyPassword(string password, string hash)
    {
        var computedHash = this.HashPassword(password);
        return computedHash == hash;
    }

    /// <summary>
    /// Restores authentication from a saved session.
    /// </summary>
    /// <param name="userId">User ID from session.</param>
    /// <returns>True if user was restored successfully.</returns>
    public async Task<bool> RestoreSessionAsync(Guid userId)
    {
        try
        {
            var user = await this.GetUserByIdAsync(userId);
            if (user != null)
            {
                this.currentUser = user;
                user.LastLoginAt = DateTime.UtcNow;
                await this.cacheService.SaveAsync(user);
                this.loggingService.LogInfo($"Session restored for user: {user.Email}");
                return true;
            }

            this.loggingService.LogWarning($"Session restore failed: User {userId} not found");
            return false;
        }
        catch (Exception ex)
        {
            this.loggingService.LogError($"Session restore failed for user {userId}", ex);
            return false;
        }
    }
}

/// <summary>
/// Interface for authentication service.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers new user.
    /// </summary>
    /// <param name="email">Email.</param>
    /// <param name="password">Password.</param>
    /// <param name="name">Name.</param>
    /// <param name="phone">Phone.</param>
    /// <returns>True if successful.</returns>
    Task<bool> RegisterAsync(string email, string password, string name, string? phone = null);

    /// <summary>
    /// Logs in user.
    /// </summary>
    /// <param name="email">Email.</param>
    /// <param name="password">Password.</param>
    /// <returns>True if successful.</returns>
    Task<bool> LoginAsync(string email, string password);

    /// <summary>
    /// Generates magic link token.
    /// </summary>
    /// <param name="email">Email.</param>
    /// <returns>Token or null.</returns>
    Task<string?> GenerateMagicLinkAsync(string email);

    /// <summary>
    /// Signs out user.
    /// </summary>
    void SignOut();

    /// <summary>
    /// Gets current user.
    /// </summary>
    /// <returns>User or null.</returns>
    CostSharing.Core.Models.User? GetCurrentUser();

    /// <summary>
    /// Checks if authenticated.
    /// </summary>
    /// <returns>True if authenticated.</returns>
    bool IsAuthenticated();

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>User or null.</returns>
    Task<CostSharing.Core.Models.User?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Gets all users in the system.
    /// </summary>
    /// <returns>List of all users.</returns>
    Task<List<CostSharing.Core.Models.User>> GetAllUsersAsync();

    /// <summary>
    /// Removes duplicate users with the same name who are not members of any group.
    /// </summary>
    /// <returns>Number of duplicate users removed.</returns>
    Task<int> RemoveDuplicateUnusedUsersAsync();

    /// <summary>
    /// Updates user profile information.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="name">New name.</param>
    /// <param name="email">New email.</param>
    /// <param name="phone">New phone.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateUserAsync(Guid userId, string name, string email, string? phone = null);

    /// <summary>
    /// Restores authentication from a saved session.
    /// </summary>
    /// <param name="userId">User ID from session.</param>
    /// <returns>True if user was restored successfully.</returns>
    Task<bool> RestoreSessionAsync(Guid userId);
}
