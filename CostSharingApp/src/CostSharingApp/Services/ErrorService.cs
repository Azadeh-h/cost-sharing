namespace CostSharingApp.Services;

/// <summary>
/// Provides user-friendly error message handling and logging.
/// </summary>
public class ErrorService : IErrorService
{
    private readonly ILoggingService? loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorService"/> class.
    /// </summary>
    /// <param name="loggingService">Optional logging service.</param>
    public ErrorService(ILoggingService? loggingService = null)
    {
        this.loggingService = loggingService;
    }

    /// <summary>
    /// Handles exception and returns user-friendly message.
    /// </summary>
    /// <param name="ex">Exception to handle.</param>
    /// <param name="context">Context description.</param>
    /// <returns>User-friendly error message.</returns>
    public string HandleException(Exception ex, string context)
    {
        this.loggingService?.LogError($"Error in {context}: {ex.Message}", ex);

        return ex switch
        {
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            InvalidOperationException => "This operation cannot be completed right now.",
            ArgumentException => "Invalid input provided. Please check your entries.",
            TimeoutException => "The operation timed out. Please check your internet connection.",
            _ => "An unexpected error occurred. Please try again."
        };
    }

    /// <summary>
    /// Validates operation result and returns error message if failed.
    /// </summary>
    /// <param name="success">Operation success status.</param>
    /// <param name="operationName">Operation description.</param>
    /// <returns>Error message or empty string if successful.</returns>
    public string GetOperationError(bool success, string operationName)
    {
        if (success)
        {
            return string.Empty;
        }

        var message = $"{operationName} failed. Please try again.";
        this.loggingService?.LogWarning(message);
        return message;
    }
}

/// <summary>
/// Interface for error handling service.
/// </summary>
public interface IErrorService
{
    /// <summary>
    /// Handles exception.
    /// </summary>
    /// <param name="ex">Exception.</param>
    /// <param name="context">Context.</param>
    /// <returns>User-friendly message.</returns>
    string HandleException(Exception ex, string context);

    /// <summary>
    /// Gets operation error message.
    /// </summary>
    /// <param name="success">Success status.</param>
    /// <param name="operationName">Operation name.</param>
    /// <returns>Error message or empty.</returns>
    string GetOperationError(bool success, string operationName);
}
