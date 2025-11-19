using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Services;

/// <summary>
/// High-performance structured logging for <see cref="DefaultToolExecutor"/> using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// These methods are compiled at build time to provide zero-allocation, high-performance logging.
/// Follow OpenTelemetry semantic conventions where applicable.
/// </remarks>
internal static partial class ToolExecutorLog
{
    // Tool Execution Lifecycle

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Executing tool '{ToolName}' (attempt {Attempt}/{MaxAttempts}, correlationId: {CorrelationId})")]
    public static partial void ExecutingTool(
        this ILogger logger,
        string toolName,
        int attempt,
        int maxAttempts,
        string correlationId);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Tool execution succeeded: '{ToolName}' completed in {DurationMs}ms (attempts: {AttemptCount})")]
    public static partial void ToolExecutionSucceeded(
        this ILogger logger,
        string toolName,
        long durationMs,
        int attemptCount);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Tool execution failed: '{ToolName}' after {AttemptCount} attempts, duration: {DurationMs}ms")]
    public static partial void ToolExecutionFailed(
        this ILogger logger,
        Exception exception,
        string toolName,
        int attemptCount,
        long durationMs);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Retrying tool execution: '{ToolName}' (attempt {Attempt}/{MaxAttempts}, delay: {DelayMs}ms, reason: {Reason})")]
    public static partial void RetryingToolExecution(
        this ILogger logger,
        string toolName,
        int attempt,
        int maxAttempts,
        int delayMs,
        string reason);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Debug,
        Message = "Tool validation started: '{ToolName}' (parameters: {ParameterCount})")]
    public static partial void ValidatingTool(
        this ILogger logger,
        string toolName,
        int parameterCount);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Warning,
        Message = "Transient error detected for '{ToolName}': {ErrorType} - {ErrorMessage}")]
    public static partial void TransientErrorDetected(
        this ILogger logger,
        string toolName,
        string errorType,
        string errorMessage);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Error,
        Message = "Terminal error detected for '{ToolName}': {ErrorType} - will not retry")]
    public static partial void TerminalErrorDetected(
        this ILogger logger,
        Exception exception,
        string toolName,
        string errorType);

    // Sequence Execution

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "Starting sequential execution of {ToolCount} tools (correlationId: {CorrelationId})")]
    public static partial void StartingSequenceExecution(
        this ILogger logger,
        int toolCount,
        string correlationId);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Sequential execution completed: {SuccessCount}/{TotalCount} tools succeeded, duration: {DurationMs}ms")]
    public static partial void SequenceExecutionCompleted(
        this ILogger logger,
        int successCount,
        int totalCount,
        long durationMs);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Sequential execution stopped at tool {Index}/{Total} due to failure: '{ToolName}'")]
    public static partial void SequenceExecutionStopped(
        this ILogger logger,
        int index,
        int total,
        string toolName);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "Executing tool {Index}/{Total} in sequence: '{ToolName}'")]
    public static partial void SequenceToolExecution(
        this ILogger logger,
        int index,
        int total,
        string toolName);

    // Fallback Strategy

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Information,
        Message = "Primary execution failed for '{ToolName}', trying {FallbackCount} fallback strategies")]
    public static partial void StartingFallbackExecution(
        this ILogger logger,
        string toolName,
        int fallbackCount);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Fallback strategy {Index}/{Total} succeeded for '{PrimaryTool}' using '{FallbackTool}'")]
    public static partial void FallbackSucceeded(
        this ILogger logger,
        int index,
        int total,
        string primaryTool,
        string fallbackTool);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "All {FallbackCount} fallback strategies failed for '{ToolName}'")]
    public static partial void AllFallbacksFailed(
        this ILogger logger,
        int fallbackCount,
        string toolName);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Debug,
        Message = "Trying fallback strategy {Index}/{Total} for '{PrimaryTool}': '{FallbackTool}'")]
    public static partial void TryingFallback(
        this ILogger logger,
        int index,
        int total,
        string primaryTool,
        string fallbackTool);

    // Validation

    [LoggerMessage(
        EventId = 4000,
        Level = LogLevel.Error,
        Message = "Tool validation failed: '{ToolName}' not found in registry (available: {AvailableTools})")]
    public static partial void ToolNotFound(
        this ILogger logger,
        string toolName,
        string availableTools);

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Error,
        Message = "Parameter validation failed for '{ToolName}': {ValidationError}")]
    public static partial void ParameterValidationFailed(
        this ILogger logger,
        string toolName,
        string validationError);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Error,
        Message = "Required parameter '{ParameterName}' not found in tool call '{ToolName}'")]
    public static partial void RequiredParameterMissing(
        this ILogger logger,
        string parameterName,
        string toolName);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Warning,
        Message = "Parameter type conversion failed for '{ParameterName}' in '{ToolName}': expected {ExpectedType}, using default")]
    public static partial void ParameterConversionFailed(
        this ILogger logger,
        string parameterName,
        string toolName,
        string expectedType);

    // Execution History

    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Debug,
        Message = "Adding execution result to history: correlationId '{CorrelationId}', tool '{ToolName}', success: {Success}")]
    public static partial void AddingToHistory(
        this ILogger logger,
        string correlationId,
        string toolName,
        bool success);

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Debug,
        Message = "Execution history trimmed for correlationId '{CorrelationId}': exceeded max size {MaxSize}")]
    public static partial void HistoryTrimmed(
        this ILogger logger,
        string correlationId,
        int maxSize);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Debug,
        Message = "Retrieving execution history for correlationId '{CorrelationId}': {ResultCount} results found")]
    public static partial void RetrievingHistory(
        this ILogger logger,
        string correlationId,
        int resultCount);

    // Browser Agent Integration

    [LoggerMessage(
        EventId = 6000,
        Level = LogLevel.Debug,
        Message = "Dispatching to browser agent: '{ToolName}' with {ParameterCount} parameters")]
    public static partial void DispatchingToBrowserAgent(
        this ILogger logger,
        string toolName,
        int parameterCount);

    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Debug,
        Message = "Browser agent operation completed: '{ToolName}' in {DurationMs}ms")]
    public static partial void BrowserAgentOperationCompleted(
        this ILogger logger,
        string toolName,
        long durationMs);

    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Warning,
        Message = "Browser agent operation timed out: '{ToolName}' after {TimeoutMs}ms")]
    public static partial void BrowserAgentTimeout(
        this ILogger logger,
        string toolName,
        int timeoutMs);

    // Configuration and Initialization

    [LoggerMessage(
        EventId = 7000,
        Level = LogLevel.Information,
        Message = "DefaultToolExecutor initialized: MaxRetries={MaxRetries}, InitialDelay={InitialDelay}ms, MaxDelay={MaxDelay}ms, TimeoutPerTool={Timeout}ms")]
    public static partial void ExecutorInitialized(
        this ILogger logger,
        int maxRetries,
        int initialDelay,
        int maxDelay,
        int timeout);

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Information,
        Message = "Exponential backoff enabled with jitter: InitialDelay={InitialDelay}ms, MaxDelay={MaxDelay}ms")]
    public static partial void ExponentialBackoffEnabled(
        this ILogger logger,
        int initialDelay,
        int maxDelay);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Debug,
        Message = "Calculated backoff delay: attempt {Attempt}, delay {DelayMs}ms (with jitter)")]
    public static partial void BackoffCalculated(
        this ILogger logger,
        int attempt,
        int delayMs);

    // Cancellation

    [LoggerMessage(
        EventId = 8000,
        Level = LogLevel.Information,
        Message = "Tool execution canceled: '{ToolName}' on attempt {Attempt}")]
    public static partial void ExecutionCanceled(
        this ILogger logger,
        string toolName,
        int attempt);

    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Information,
        Message = "Sequence execution canceled at tool {Index}/{Total}: '{ToolName}'")]
    public static partial void SequenceCanceled(
        this ILogger logger,
        int index,
        int total,
        string toolName);
}
