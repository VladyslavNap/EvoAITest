namespace EvoAITest.Core.Models;

/// <summary>
/// Represents the result of executing a single browser automation tool, including success status,
/// return data, error details, execution metrics, and retry statistics.
/// </summary>
/// <remarks>
/// <para>
/// This record provides comprehensive information about tool execution outcomes, making it suitable
/// for debugging, analysis, logging, and self-healing agent feedback loops.
/// </para>
/// <para>
/// All instances are immutable and thread-safe. Use the factory methods <see cref="Succeeded"/>
/// and <see cref="Failed"/> to create instances with appropriate defaults.
/// </para>
/// </remarks>
/// <param name="Success">
/// Indicates whether the tool execution completed successfully. True means the tool executed
/// without errors and met its success criteria. False indicates a failure that could not be
/// recovered through retries or fallbacks.
/// </param>
/// <param name="ToolName">
/// The name of the tool that was executed (e.g., "navigate", "click", "type"). This matches
/// the tool name in the <see cref="BrowserToolRegistry"/>.
/// </param>
/// <param name="Result">
/// The data returned by the tool execution. The type varies by tool:
/// - navigate: null or navigation result
/// - click: null (success indicated by Success property)
/// - type: null (success indicated by Success property)
/// - extract_text: string containing extracted text
/// - get_page_state: PageState object
/// - take_screenshot: base64-encoded image string
/// - verify_element_exists: bool indicating existence
/// Can be null for tools that don't return data.
/// </param>
/// <param name="Error">
/// The exception that caused the execution to fail, if applicable. Null if execution succeeded.
/// Includes the full exception chain for detailed debugging. For security, avoid exposing
/// sensitive information in exception messages when returning results to clients.
/// </param>
/// <param name="ExecutionDuration">
/// The total time taken to execute the tool, including all retry attempts. This measures
/// wall-clock time from the start of the first attempt to completion (success or final failure).
/// Useful for performance analysis and timeout tuning.
/// </param>
/// <param name="AttemptCount">
/// The total number of execution attempts made, including the initial attempt and all retries.
/// A value of 1 indicates the tool succeeded or failed on the first attempt with no retries.
/// Higher values indicate the tool required multiple attempts due to transient failures.
/// </param>
/// <param name="Metadata">
/// Additional execution context and metadata as key-value pairs. Common metadata includes:
/// - "correlation_id": string - Correlation ID for distributed tracing
/// - "selector": string - CSS selector used (for element-based tools)
/// - "url": string - URL at time of execution
/// - "page_title": string - Page title at time of execution
/// - "retry_reasons": string[] - Reasons for each retry attempt
/// - "fallback_used": bool - Whether a fallback strategy was used
/// - "primary_error": string - Error from primary execution when fallback succeeded
/// - "browser_state": object - Snapshot of browser state at execution time
/// - "screenshots": string[] - Base64-encoded screenshots from failed attempts
/// Extensible for custom metrics and context. Never null, but may be empty.
/// </param>
public sealed record ToolExecutionResult(
    bool Success,
    string ToolName,
    object? Result,
    Exception? Error,
    TimeSpan ExecutionDuration,
    int AttemptCount,
    Dictionary<string, object> Metadata
)
{
    /// <summary>
    /// Gets a value indicating whether the execution failed.
    /// </summary>
    /// <remarks>
    /// Convenience property that returns the inverse of <see cref="Success"/>.
    /// Useful for clearer conditional logic in error handling code.
    /// </remarks>
    public bool IsFailure => !Success;

    /// <summary>
    /// Gets a value indicating whether the tool was retried.
    /// </summary>
    /// <remarks>
    /// Returns true if <see cref="AttemptCount"/> is greater than 1, indicating
    /// the tool required retry attempts beyond the initial execution.
    /// </remarks>
    public bool WasRetried => AttemptCount > 1;

    /// <summary>
    /// Gets a human-readable summary of the execution result.
    /// </summary>
    /// <remarks>
    /// Provides a concise description of the outcome, including tool name, success status,
    /// duration, and attempt count. Suitable for logging and user-facing messages.
    /// </remarks>
    public string Summary =>
        Success
            ? $"Tool '{ToolName}' succeeded in {ExecutionDuration.TotalMilliseconds:F0}ms ({AttemptCount} attempt(s))"
            : $"Tool '{ToolName}' failed after {AttemptCount} attempt(s): {Error?.Message ?? "Unknown error"}";

    /// <summary>
    /// Creates a successful tool execution result with the specified data.
    /// </summary>
    /// <param name="toolName">The name of the tool that was executed successfully.</param>
    /// <param name="result">Optional data returned by the tool execution.</param>
    /// <param name="executionDuration">The time taken to execute the tool.</param>
    /// <param name="attemptCount">The number of execution attempts made (default: 1).</param>
    /// <param name="metadata">Optional metadata about the execution (default: empty dictionary).</param>
    /// <returns>
    /// A new <see cref="ToolExecutionResult"/> instance representing a successful execution.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this factory method to create success results consistently across the codebase.
    /// It ensures all required properties are set correctly and provides sensible defaults.
    /// </para>
    /// </remarks>
    public static ToolExecutionResult Succeeded(
        string toolName,
        object? result = null,
        TimeSpan? executionDuration = null,
        int attemptCount = 1,
        Dictionary<string, object>? metadata = null)
    {
        return new ToolExecutionResult(
            Success: true,
            ToolName: toolName,
            Result: result,
            Error: null,
            ExecutionDuration: executionDuration ?? TimeSpan.Zero,
            AttemptCount: Math.Max(1, attemptCount),
            Metadata: metadata ?? new Dictionary<string, object>()
        );
    }

    /// <summary>
    /// Creates a failed tool execution result with the specified error details.
    /// </summary>
    /// <param name="toolName">The name of the tool that failed.</param>
    /// <param name="error">The exception that caused the failure.</param>
    /// <param name="executionDuration">The time spent attempting to execute the tool.</param>
    /// <param name="attemptCount">The number of execution attempts made before giving up.</param>
    /// <param name="metadata">Optional metadata about the failed execution (default: empty dictionary).</param>
    /// <returns>
    /// A new <see cref="ToolExecutionResult"/> instance representing a failed execution.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this factory method to create failure results consistently across the codebase.
    /// It ensures error information is properly captured for debugging and logging.
    /// </para>
    /// <para>
    /// The <paramref name="error"/> parameter should contain the root cause exception,
    /// not a wrapper exception, for more accurate error analysis and recovery strategies.
    /// </para>
    /// </remarks>
    public static ToolExecutionResult Failed(
        string toolName,
        Exception error,
        TimeSpan? executionDuration = null,
        int attemptCount = 1,
        Dictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        var failureMetadata = metadata ?? new Dictionary<string, object>();
        
        // Add error context to metadata if not already present
        if (!failureMetadata.ContainsKey("error_type"))
        {
            failureMetadata["error_type"] = error.GetType().Name;
        }
        
        if (!failureMetadata.ContainsKey("error_message"))
        {
            failureMetadata["error_message"] = error.Message;
        }

        return new ToolExecutionResult(
            Success: false,
            ToolName: toolName,
            Result: null,
            Error: error,
            ExecutionDuration: executionDuration ?? TimeSpan.Zero,
            AttemptCount: Math.Max(1, attemptCount),
            Metadata: failureMetadata
        );
    }

    /// <summary>
    /// Creates a new result with additional metadata merged into the existing metadata.
    /// </summary>
    /// <param name="additionalMetadata">Metadata to add or update. Existing keys will be overwritten.</param>
    /// <returns>A new <see cref="ToolExecutionResult"/> with the merged metadata.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a new instance with merged metadata, preserving immutability.
    /// Use this to add context to results as they flow through processing pipelines,
    /// such as adding retry reasons, fallback information, or healing strategy details.
    /// </para>
    /// </remarks>
    public ToolExecutionResult WithMetadata(Dictionary<string, object> additionalMetadata)
    {
        ArgumentNullException.ThrowIfNull(additionalMetadata);

        var mergedMetadata = new Dictionary<string, object>(Metadata);
        foreach (var (key, value) in additionalMetadata)
        {
            mergedMetadata[key] = value;
        }

        return this with { Metadata = mergedMetadata };
    }

    /// <summary>
    /// Creates a new result with a single metadata item added or updated.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A new <see cref="ToolExecutionResult"/> with the added metadata.</returns>
    /// <remarks>
    /// Convenience method for adding a single metadata item without creating a dictionary.
    /// </remarks>
    public ToolExecutionResult WithMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return WithMetadata(new Dictionary<string, object> { [key] = value });
    }
}
