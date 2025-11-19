namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration options for the <see cref="Abstractions.IToolExecutor"/> service, controlling
/// retry behavior, backoff strategy, concurrency limits, timeouts, and telemetry.
/// </summary>
/// <remarks>
/// <para>
/// Configure these options in appsettings.json under the "EvoAITest:ToolExecutor" section:
/// <code>
/// {
///   "EvoAITest": {
///     "ToolExecutor": {
///       "MaxRetries": 3,
///       "InitialRetryDelayMs": 500,
///       "MaxRetryDelayMs": 10000,
///       "UseExponentialBackoff": true,
///       "MaxConcurrentTools": 1,
///       "TimeoutPerToolMs": 30000,
///       "EnableDetailedLogging": true
///     }
///   }
/// }
/// </code>
/// </para>
/// <para>
/// For production deployments, consider:
/// - Reducing <see cref="MaxRetries"/> to 2 for faster failure detection
/// - Increasing <see cref="TimeoutPerToolMs"/> to 60000 for slow external sites
/// - Setting <see cref="EnableDetailedLogging"/> to false to reduce log volume
/// - Adjusting <see cref="MaxRetryDelayMs"/> based on site load times
/// </para>
/// <para>
/// For development, use:
/// - <see cref="EnableDetailedLogging"/> = true for debugging
/// - <see cref="MaxRetries"/> = 1 for fast failure (no retries)
/// - Lower <see cref="TimeoutPerToolMs"/> for quicker feedback
/// </para>
/// </remarks>
public sealed class ToolExecutorOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed tool executions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 3 retries (total of 4 attempts: 1 initial + 3 retries)
    /// </para>
    /// <para>
    /// A value of 0 means no retries (single attempt only). Minimum value is 0, maximum is 10.
    /// Higher retry counts increase resilience to transient failures (network issues, race conditions)
    /// but also increase execution time and resource usage on persistent failures.
    /// </para>
    /// <para>
    /// Recommended values:
    /// - Production: 2-3 retries
    /// - Development: 0-1 retry (fail fast)
    /// - CI/CD: 1 retry (balance speed and reliability)
    /// </para>
    /// <para>
    /// Retries apply per tool execution. For sequences, each tool gets its own retry budget.
    /// </para>
    /// </remarks>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay in milliseconds between retry attempts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 500ms
    /// </para>
    /// <para>
    /// This is the base delay for the first retry. Subsequent retries may use exponential backoff
    /// (if <see cref="UseExponentialBackoff"/> is true), multiplying this value by powers of 2.
    /// </para>
    /// <para>
    /// Minimum value: 100ms (prevents tight retry loops)
    /// Maximum value: 5000ms
    /// </para>
    /// <para>
    /// Exponential backoff progression (with jitter):
    /// - Attempt 1: 0ms (initial attempt)
    /// - Retry 1: ~500ms (InitialRetryDelayMs × 2^0 with jitter)
    /// - Retry 2: ~1000ms (InitialRetryDelayMs × 2^1 with jitter)
    /// - Retry 3: ~2000ms (InitialRetryDelayMs × 2^2 with jitter)
    /// </para>
    /// <para>
    /// For slower sites or APIs, increase this value. For local development, decrease to 100-200ms.
    /// </para>
    /// </remarks>
    public int InitialRetryDelayMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum delay in milliseconds between retry attempts, regardless of exponential backoff.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 10000ms (10 seconds)
    /// </para>
    /// <para>
    /// This caps the retry delay to prevent exponentially growing wait times from becoming impractical.
    /// Even with aggressive exponential backoff, no retry will wait longer than this value.
    /// </para>
    /// <para>
    /// Minimum value: 1000ms
    /// Maximum value: 60000ms (1 minute)
    /// </para>
    /// <para>
    /// Formula: actualDelay = Min(exponentialDelay, MaxRetryDelayMs)
    /// </para>
    /// <para>
    /// This setting is particularly important for:
    /// - Preventing excessive wait times in automation workflows
    /// - Keeping total execution time predictable
    /// - Avoiding timeout conflicts with higher-level orchestration
    /// </para>
    /// </remarks>
    public int MaxRetryDelayMs { get; set; } = 10000;

    /// <summary>
    /// Gets or sets a value indicating whether to use exponential backoff for retry delays.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: true (recommended for production)
    /// </para>
    /// <para>
    /// When enabled, retry delays increase exponentially with each attempt:
    /// - Retry 1: InitialRetryDelayMs × 2^0 = 500ms
    /// - Retry 2: InitialRetryDelayMs × 2^1 = 1000ms
    /// - Retry 3: InitialRetryDelayMs × 2^2 = 2000ms
    /// </para>
    /// <para>
    /// Jitter is automatically applied (±25%) to prevent thundering herd problems when multiple
    /// tools fail simultaneously and retry at the same intervals.
    /// </para>
    /// <para>
    /// When disabled, all retry delays are equal to <see cref="InitialRetryDelayMs"/>:
    /// - Retry 1: 500ms
    /// - Retry 2: 500ms
    /// - Retry 3: 500ms
    /// </para>
    /// <para>
    /// Exponential backoff is recommended for most scenarios as it:
    /// - Gives transient issues time to resolve
    /// - Reduces load on failing systems
    /// - Aligns with Microsoft's retry guidance
    /// </para>
    /// <para>
    /// Disable exponential backoff for:
    /// - Deterministic testing scenarios
    /// - Very fast operations where fixed delays are sufficient
    /// - Debugging retry behavior
    /// </para>
    /// <para>
    /// Reference: https://learn.microsoft.com/en-us/azure/architecture/patterns/retry
    /// </para>
    /// </remarks>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of browser automation tools that can execute concurrently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 1 (sequential execution only)
    /// </para>
    /// <para>
    /// This setting controls how many tools can interact with the browser simultaneously.
    /// For most browser automation scenarios, concurrent tool execution is not safe because:
    /// - Browser state is shared (URL, page, DOM)
    /// - Element references become stale
    /// - Race conditions in page interactions
    /// </para>
    /// <para>
    /// However, for specific scenarios with multiple independent browsers or iframe contexts,
    /// concurrent execution may be beneficial. Increase this value only when:
    /// - Tools operate on different browser instances
    /// - Tools target different iframes or windows
    /// - You have implemented proper synchronization
    /// </para>
    /// <para>
    /// Minimum value: 1 (sequential execution)
    /// Maximum value: 10
    /// Recommended: 1 (safe default)
    /// </para>
    /// <para>
    /// This setting does NOT apply to <see cref="Abstractions.IToolExecutor.ExecuteSequenceAsync"/>,
    /// which always executes sequentially. It only affects hypothetical parallel execution methods.
    /// </para>
    /// </remarks>
    public int MaxConcurrentTools { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for each individual tool execution attempt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 30000ms (30 seconds)
    /// </para>
    /// <para>
    /// This timeout applies to each execution attempt, not the total time including retries.
    /// If a tool exceeds this timeout, it is canceled and considered a failed attempt,
    /// potentially triggering a retry if retries are configured.
    /// </para>
    /// <para>
    /// Total possible execution time = TimeoutPerToolMs × (MaxRetries + 1) + total retry delays
    /// </para>
    /// <para>
    /// Minimum value: 5000ms (5 seconds)
    /// Maximum value: 300000ms (5 minutes)
    /// </para>
    /// <para>
    /// Recommended values by tool type:
    /// - Navigate, wait_for_element: 30000ms (30s)
    /// - Click, type, fill: 10000ms (10s)
    /// - Extract data: 15000ms (15s)
    /// - Take screenshot: 20000ms (20s)
    /// </para>
    /// <para>
    /// For slow external sites or complex SPAs with heavy JavaScript, consider increasing to
    /// 60000ms. For internal sites or fast networks, 10000-15000ms may be sufficient.
    /// </para>
    /// <para>
    /// This timeout is distinct from <see cref="EvoAITestCoreOptions.BrowserTimeoutMs"/>.
    /// The browser timeout controls individual browser operations (element waits, clicks),
    /// while this timeout controls the entire tool execution including setup and teardown.
    /// </para>
    /// </remarks>
    public int TimeoutPerToolMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging for tool executions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: true
    /// </para>
    /// <para>
    /// When enabled, the tool executor logs:
    /// - Start and completion of each tool execution
    /// - Detailed retry information (attempt number, reason, delay)
    /// - Parameter values and tool call context
    /// - Execution duration and performance metrics
    /// - Success/failure outcomes with stack traces
    /// - Correlation IDs for distributed tracing
    /// </para>
    /// <para>
    /// When disabled, only critical errors and summary information are logged, reducing
    /// log volume by approximately 70-80%.
    /// </para>
    /// <para>
    /// Enable for:
    /// - Development and debugging
    /// - Integration testing
    /// - Production troubleshooting (temporary)
    /// - Performance analysis
    /// </para>
    /// <para>
    /// Disable for:
    /// - High-volume production workloads
    /// - Cost-sensitive logging (e.g., Azure Log Analytics)
    /// - GDPR/compliance scenarios where parameter logging is restricted
    /// </para>
    /// <para>
    /// Even when disabled, telemetry data is still emitted to OpenTelemetry traces and metrics.
    /// This setting only affects text-based logging via ILogger.
    /// </para>
    /// </remarks>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of execution results to keep in the in-memory history per correlation ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 100
    /// </para>
    /// <para>
    /// This prevents unbounded memory growth when tracking execution history via
    /// <see cref="Abstractions.IToolExecutor.GetExecutionHistoryAsync"/>. Once the limit is reached,
    /// the oldest execution results are discarded (FIFO).
    /// </para>
    /// <para>
    /// Minimum value: 10
    /// Maximum value: 1000
    /// </para>
    /// <para>
    /// Adjust based on:
    /// - Expected workflow complexity (number of tools per task)
    /// - Memory constraints in containerized environments
    /// - Need for historical analysis
    /// </para>
    /// <para>
    /// Note: This is in-memory storage only. For persistent history across application restarts,
    /// implement a separate persistence layer (database, blob storage, etc.).
    /// </para>
    /// </remarks>
    public int MaxHistorySize { get; set; } = 100;

    /// <summary>
    /// Validates the configuration and throws an exception if any settings are invalid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method during application startup (e.g., in Program.cs) to fail fast if
    /// configuration is incorrect:
    /// <code>
    /// var options = app.Services.GetRequiredService&lt;IOptions&lt;ToolExecutorOptions&gt;&gt;().Value;
    /// options.Validate();
    /// </code>
    /// </para>
    /// <para>
    /// This method checks:
    /// - All numeric values are within acceptable ranges
    /// - Timeout values are reasonable for automation scenarios
    /// - Retry configuration is consistent
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any configuration value is outside its valid range or when configuration
    /// contains logical inconsistencies.
    /// </exception>
    public void Validate()
    {
        if (MaxRetries < 0 || MaxRetries > 10)
        {
            throw new InvalidOperationException(
                $"MaxRetries must be between 0 and 10. Current value: {MaxRetries}. " +
                "Recommended: 2-3 for production, 0-1 for development.");
        }

        if (InitialRetryDelayMs < 100 || InitialRetryDelayMs > 5000)
        {
            throw new InvalidOperationException(
                $"InitialRetryDelayMs must be between 100 and 5000. Current value: {InitialRetryDelayMs}ms. " +
                "Recommended: 500ms for most scenarios, 100-200ms for fast local development.");
        }

        if (MaxRetryDelayMs < 1000 || MaxRetryDelayMs > 60000)
        {
            throw new InvalidOperationException(
                $"MaxRetryDelayMs must be between 1000 and 60000. Current value: {MaxRetryDelayMs}ms. " +
                "Recommended: 10000ms (10 seconds) for most scenarios.");
        }

        if (MaxRetryDelayMs < InitialRetryDelayMs)
        {
            throw new InvalidOperationException(
                $"MaxRetryDelayMs ({MaxRetryDelayMs}ms) must be greater than or equal to " +
                $"InitialRetryDelayMs ({InitialRetryDelayMs}ms).");
        }

        if (MaxConcurrentTools < 1 || MaxConcurrentTools > 10)
        {
            throw new InvalidOperationException(
                $"MaxConcurrentTools must be between 1 and 10. Current value: {MaxConcurrentTools}. " +
                "Recommended: 1 (sequential execution) for most browser automation scenarios.");
        }

        if (TimeoutPerToolMs < 5000 || TimeoutPerToolMs > 300000)
        {
            throw new InvalidOperationException(
                $"TimeoutPerToolMs must be between 5000 and 300000. Current value: {TimeoutPerToolMs}ms. " +
                "Recommended: 30000ms (30 seconds) for most scenarios, increase for slow sites.");
        }

        if (MaxHistorySize < 10 || MaxHistorySize > 1000)
        {
            throw new InvalidOperationException(
                $"MaxHistorySize must be between 10 and 1000. Current value: {MaxHistorySize}. " +
                "Recommended: 100 for most scenarios.");
        }

        // Calculate and validate total possible execution time
        var maxTotalTimeMs = TimeoutPerToolMs * (MaxRetries + 1) + 
                            (UseExponentialBackoff 
                                ? CalculateMaxExponentialBackoffTime() 
                                : InitialRetryDelayMs * MaxRetries);

        if (maxTotalTimeMs > 600000) // 10 minutes
        {
            throw new InvalidOperationException(
                $"Total maximum execution time ({maxTotalTimeMs}ms = {maxTotalTimeMs / 1000}s) exceeds 10 minutes. " +
                $"This is too long for automated testing. Consider reducing MaxRetries ({MaxRetries}), " +
                $"TimeoutPerToolMs ({TimeoutPerToolMs}ms), or MaxRetryDelayMs ({MaxRetryDelayMs}ms).");
        }
    }

    private int CalculateMaxExponentialBackoffTime()
    {
        var totalDelay = 0;
        for (int i = 0; i < MaxRetries; i++)
        {
            var exponentialDelay = InitialRetryDelayMs * Math.Pow(2, i);
            var cappedDelay = Math.Min(exponentialDelay, MaxRetryDelayMs);
            totalDelay += (int)cappedDelay;
        }
        return totalDelay;
    }
}
