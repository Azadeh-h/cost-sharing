
using System.Security.Cryptography;
using System.Text;
using CostSharing.Core.Models;

namespace CostSharingApp.Services;
/// <summary>
/// Provides authentication services including email/password and magic link.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ICacheService cacheService;
    private readonly ILoggingService loggingService;
    private CostSharing.Core.Models.User? currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service for credential storage.</param>
    /// <param name="loggingService">Logging service.</param>
    public AuthService(ICacheService cacheService, ILoggingService loggingService)
    {
        this.cacheService = cacheService;
        this.loggingService = loggingService;
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
            // Check if user already exists
            var existingUsers = await this.cacheService.GetAllAsync<CostSharing.Core.Models.User>();
            if (existingUsers.Any(u => u.Email == email))
            {
                this.loggingService.LogWarning($"Registration failed: Email {email} already exists");
                return false;
            }

            var user = new CostSharing.Core.Models.User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = this.HashPassword(password),
                Name = name,
                Phone = phone,
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = false
            };

            await this.cacheService.SaveAsync(user);
            this.currentUser = user;
            this.loggingService.LogInfo($"User registered: {email}");
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
            var users = await this.cacheService.GetAllAsync<CostSharing.Core.Models.User>();
            var user = users.FirstOrDefault(u => u.Email == email);

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
}
