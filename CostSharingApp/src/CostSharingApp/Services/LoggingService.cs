
using System.Diagnostics;

namespace CostSharingApp.Services;
/// <summary>
/// Provides structured logging for application diagnostics.
/// </summary>
public class LoggingService : ILoggingService
{
    /// <summary>
    /// Logs informational message.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public void LogInfo(string message)
    {
        Debug.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }

    /// <summary>
    /// Logs warning message.
    /// </summary>
    /// <param name="message">Warning message.</param>
    public void LogWarning(string message)
    {
        Debug.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }

    /// <summary>
    /// Logs error with exception details.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="exception">Exception details.</param>
    public void LogError(string message, Exception? exception = null)
    {
        Debug.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        if (exception != null)
        {
            Debug.WriteLine($"  Exception: {exception.GetType().Name}");
            Debug.WriteLine($"  Message: {exception.Message}");
            Debug.WriteLine($"  StackTrace: {exception.StackTrace}");
        }
    }

    /// <summary>
    /// Logs debug information (development only).
    /// </summary>
    /// <param name="message">Debug message.</param>
    public void LogDebug(string message)
    {
#if DEBUG
        Debug.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
#endif
    }
}

/// <summary>
/// Interface for logging service.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Logs info message.
    /// </summary>
    /// <param name="message">Message.</param>
    void LogInfo(string message);

    /// <summary>
    /// Logs warning.
    /// </summary>
    /// <param name="message">Message.</param>
    void LogWarning(string message);

    /// <summary>
    /// Logs error.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="exception">Exception.</param>
    void LogError(string message, Exception? exception = null);

    /// <summary>
    /// Logs debug info.
    /// </summary>
    /// <param name="message">Message.</param>
    void LogDebug(string message);
}
