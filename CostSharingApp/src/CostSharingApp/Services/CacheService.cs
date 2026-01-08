namespace CostSharingApp.Services;
using SQLite;

/// <summary>
/// Provides local SQLite caching for offline support and sync queue.
/// </summary>
public class CacheService : ICacheService
{
    private readonly string databasePath;
    private SQLiteAsyncConnection? database;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    public CacheService()
    {
        this.databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "costsharingcache.db3");
    }

    /// <summary>
    /// Initializes database connection and creates tables.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    public async Task InitializeAsync()
    {
        if (this.database != null)
        {
            return;
        }

        this.database = new SQLiteAsyncConnection(this.databasePath);

        // Create tables for cached entities
        await this.database.CreateTableAsync<CostSharing.Core.Models.User>();
        await this.database.CreateTableAsync<CostSharing.Core.Models.Group>();
        await this.database.CreateTableAsync<CostSharing.Core.Models.GroupMember>();
        await this.database.CreateTableAsync<CostSharing.Core.Models.Expense>();
        await this.database.CreateTableAsync<CostSharing.Core.Models.ExpenseSplit>();
        await this.database.CreateTableAsync<CostSharing.Core.Models.Debt>();
        await this.database.CreateTableAsync<CostSharing.Core.Models.Settlement>();
        await this.database.CreateTableAsync<CostSharing.Core.Models.Invitation>();
    }

    /// <summary>
    /// Saves item to cache.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="item">Item to cache.</param>
    /// <returns>Task for async operation.</returns>
    public async Task SaveAsync<T>(T item)
        where T : class, new()
    {
        if (this.database == null)
        {
            await this.InitializeAsync();
        }

        await this.database!.InsertOrReplaceAsync(item);
    }

    /// <summary>
    /// Gets item from cache by ID.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="id">Entity ID.</param>
    /// <returns>Cached item or null.</returns>
    public async Task<T?> GetAsync<T>(Guid id)
        where T : class, new()
    {
        if (this.database == null)
        {
            await this.InitializeAsync();
        }

        return await this.database!.FindAsync<T>(id);
    }

    /// <summary>
    /// Gets all items of type T from cache.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <returns>List of cached items.</returns>
    public async Task<List<T>> GetAllAsync<T>()
        where T : class, new()
    {
        if (this.database == null)
        {
            await this.InitializeAsync();
        }

        return await this.database!.Table<T>().ToListAsync();
    }

    /// <summary>
    /// Deletes item from cache.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="item">Item to delete.</param>
    /// <returns>Task for async operation.</returns>
    public async Task DeleteAsync<T>(T item)
        where T : class, new()
    {
        if (this.database == null)
        {
            await this.InitializeAsync();
        }

        await this.database!.DeleteAsync(item);
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    /// <returns>Task for async operation.</returns>
    public async Task ClearAllAsync()
    {
        if (this.database == null)
        {
            await this.InitializeAsync();
        }

        await this.database!.DropTableAsync<CostSharing.Core.Models.User>();
        await this.database!.DropTableAsync<CostSharing.Core.Models.Group>();
        await this.database!.DropTableAsync<CostSharing.Core.Models.GroupMember>();
        await this.database!.DropTableAsync<CostSharing.Core.Models.Expense>();
        await this.database!.DropTableAsync<CostSharing.Core.Models.ExpenseSplit>();
        await this.database!.DropTableAsync<CostSharing.Core.Models.Debt>();
        await this.database!.DropTableAsync<CostSharing.Core.Models.Settlement>();
        await this.database!.DropTableAsync<CostSharing.Core.Models.Invitation>();

        await this.InitializeAsync();
    }
}

/// <summary>
/// Interface for local caching service.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Initializes database.
    /// </summary>
    /// <returns>Task.</returns>
    Task InitializeAsync();

    /// <summary>
    /// Saves item to cache.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="item">Item to save.</param>
    /// <returns>Task.</returns>
    Task SaveAsync<T>(T item)
        where T : class, new();

    /// <summary>
    /// Gets item from cache.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="id">Item ID.</param>
    /// <returns>Item or null.</returns>
    Task<T?> GetAsync<T>(Guid id)
        where T : class, new();

    /// <summary>
    /// Gets all items of type.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <returns>List of items.</returns>
    Task<List<T>> GetAllAsync<T>()
        where T : class, new();

    /// <summary>
    /// Deletes item.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="item">Item to delete.</param>
    /// <returns>Task.</returns>
    Task DeleteAsync<T>(T item)
        where T : class, new();

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    /// <returns>Task.</returns>
    Task ClearAllAsync();
}
