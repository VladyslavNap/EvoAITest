namespace EvoAITest.Core.Models;

/// <summary>
/// Represents the result of executing a browser action or automation task.
/// </summary>
public sealed class ExecutionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the action ID that was executed.
    /// </summary>
    public string? ActionId { get; set; }

    /// <summary>
    /// Gets or sets the extracted data or return value from the action.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception details if an error occurred.
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Gets or sets the screenshot data (base64) captured during execution.
    /// </summary>
    public string? Screenshot { get; set; }

    /// <summary>
    /// Gets or sets the DOM state or HTML captured during execution.
    /// </summary>
    public string? DomState { get; set; }

    /// <summary>
    /// Gets or sets metadata about the execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when execution started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when execution completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets retry information if the action was retried.
    /// </summary>
    public RetryInfo? RetryInfo { get; set; }

    /// <summary>
    /// Creates a successful execution result.
    /// </summary>
    /// <param name="actionId">The action ID.</param>
    /// <param name="data">Optional result data.</param>
    /// <returns>A successful ExecutionResult.</returns>
    public static ExecutionResult Succeeded(string actionId, object? data = null) =>
        new()
        {
            Success = true,
            ActionId = actionId,
            Data = data,
            CompletedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Creates a failed execution result.
    /// </summary>
    /// <param name="actionId">The action ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorDetails">Optional error details.</param>
    /// <returns>A failed ExecutionResult.</returns>
    public static ExecutionResult Failed(string actionId, string errorMessage, string? errorDetails = null) =>
        new()
        {
            Success = false,
            ActionId = actionId,
            ErrorMessage = errorMessage,
            ErrorDetails = errorDetails,
            CompletedAt = DateTimeOffset.UtcNow
        };
}

/// <summary>
/// Represents retry information for an action execution.
/// </summary>
public sealed class RetryInfo
{
    /// <summary>
    /// Gets or sets the number of retry attempts made.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retries allowed.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the reason for the last retry.
    /// </summary>
    public string? LastRetryReason { get; set; }

    /// <summary>
    /// Gets or sets the total time spent retrying in milliseconds.
    /// </summary>
    public long TotalRetryTimeMs { get; set; }
}
