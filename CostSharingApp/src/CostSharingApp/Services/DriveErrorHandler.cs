namespace CostSharingApp.Services;

using Google;
using System.Net;

/// <summary>
/// Helper class for handling Google Drive API errors with exponential backoff.
/// </summary>
public static class DriveErrorHandler
{
    private const int MaxRetries = 3;
    private const int BaseDelayMs = 1000;

    /// <summary>
    /// Executes a Drive API operation with exponential backoff retry logic.
    /// </summary>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        ILoggingService? logger = null,
        CancellationToken cancellationToken = default)
    {
        int retryCount = 0;

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (GoogleApiException ex) when (IsTransientError(ex))
            {
                retryCount++;

                if (retryCount > MaxRetries)
                {
                    logger?.LogError($"Max retries ({MaxRetries}) exceeded for Drive operation", ex);
                    throw new InvalidOperationException(
                        "Google Drive sync failed after multiple retries. Please check your internet connection and try again.",
                        ex);
                }

                var delayMs = BaseDelayMs * (int)Math.Pow(2, retryCount - 1);
                logger?.LogWarning($"Transient Drive error (attempt {retryCount}/{MaxRetries}), retrying in {delayMs}ms: {ex.Message}");

                await Task.Delay(delayMs, cancellationToken);
            }
            catch (GoogleApiException ex)
            {
                logger?.LogError($"Drive API error: {ex.HttpStatusCode} - {ex.Message}", ex);
                throw TranslateException(ex);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Unexpected error during Drive operation: {ex.Message}", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Determines if a Google API exception is transient and should be retried.
    /// </summary>
    private static bool IsTransientError(GoogleApiException ex)
    {
        return ex.HttpStatusCode == HttpStatusCode.TooManyRequests || // 429 Rate Limit
               ex.HttpStatusCode == HttpStatusCode.InternalServerError || // 500
               ex.HttpStatusCode == HttpStatusCode.BadGateway || // 502
               ex.HttpStatusCode == HttpStatusCode.ServiceUnavailable || // 503
               ex.HttpStatusCode == HttpStatusCode.GatewayTimeout; // 504
    }

    /// <summary>
    /// Translates Google API exceptions into user-friendly error messages.
    /// </summary>
    private static Exception TranslateException(GoogleApiException ex)
    {
        return ex.HttpStatusCode switch
        {
            HttpStatusCode.Unauthorized => // 401
                new UnauthorizedAccessException(
                    "Your Google Drive authorization has expired. Please authorize again in Settings.",
                    ex),

            HttpStatusCode.Forbidden => // 403
                new UnauthorizedAccessException(
                    "Access denied. You may not have permission to access this Drive folder.",
                    ex),

            HttpStatusCode.NotFound => // 404
                new InvalidOperationException(
                    "The Drive folder was not found. It may have been deleted. Please disable and re-enable sync.",
                    ex),

            HttpStatusCode.TooManyRequests => // 429
                new InvalidOperationException(
                    "Google Drive rate limit exceeded. Please wait a few minutes and try again.",
                    ex),

            HttpStatusCode.InsufficientStorage => // 507
                new InvalidOperationException(
                    "Your Google Drive storage is full. Please free up space and try again.",
                    ex),

            _ => new InvalidOperationException(
                $"Drive sync failed: {ex.Message}. Please try again later.",
                ex)
        };
    }
}
