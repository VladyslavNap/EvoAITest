using EvoAITest.Core.Models;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Defines the contract for executing browser automation tools with retry, backoff, and recovery semantics.
/// This service coordinates tool invocations using <see cref="IBrowserAgent"/> and validates tools against
/// the <see cref="BrowserToolRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// The tool executor is responsible for:
/// - Validating tool definitions against the registry
/// - Executing tools with configurable retry logic
/// - Implementing exponential backoff with jitter
/// - Tracking execution metrics and durations
/// - Supporting sequential and parallel execution patterns
/// - Providing fallback and recovery mechanisms
/// - Coordinating cancellation across tool sequences
/// </para>
/// <para>
/// This interface is designed for .NET Aspire containerized environments and supports
/// graceful shutdown via <see cref="CancellationToken"/>. All methods are thread-safe
/// and can be called concurrently.
/// </para>
/// <para>
/// Register as a scoped service in the DI container to ensure proper resource management
/// and correlation context across tool executions within a single automation task.
/// </para>
/// </remarks>
/// <example>
/// Basic usage with a single tool:
/// <code>
/// var toolCall = new ToolCall(
///     ToolName: "navigate",
///     Parameters: new Dictionary&lt;string, object&gt; { ["url"] = "https://example.com" },
///     Reasoning: "Navigate to the target website",
///     CorrelationId: correlationId
/// );
/// 
/// var result = await executor.ExecuteToolAsync(toolCall, cancellationToken);
/// 
/// if (result.Success)
/// {
///     Console.WriteLine($"Navigation completed in {result.ExecutionDuration.TotalSeconds}s");
/// }
/// else
/// {
///     Console.WriteLine($"Navigation failed after {result.AttemptCount} attempts: {result.Error?.Message}");
/// }
/// </code>
/// </example>
public interface IToolExecutor
{
    /// <summary>
    /// Executes a single browser automation tool with retry and backoff semantics.
    /// </summary>
    /// <param name="toolCall">
    /// The tool call containing the tool name, parameters, reasoning, and correlation ID.
    /// The tool name must exist in the <see cref="BrowserToolRegistry"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation. Honors cancellation between retry attempts
    /// and propagates to the underlying browser agent operations.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="ToolExecutionResult"/> with success status, result data, error details,
    /// execution metrics, and retry statistics.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Execution flow:
    /// 1. Validate tool exists in registry
    /// 2. Validate required parameters are provided
    /// 3. Execute tool via <see cref="IBrowserAgent"/>
    /// 4. On failure, retry with exponential backoff (if configured)
    /// 5. Return comprehensive result with metrics
    /// </para>
    /// <para>
    /// Retry behavior is controlled by <see cref="Options.ToolExecutorOptions.MaxRetries"/>,
    /// <see cref="Options.ToolExecutorOptions.InitialRetryDelayMs"/>, and
    /// <see cref="Options.ToolExecutorOptions.UseExponentialBackoff"/>.
    /// </para>
    /// <para>
    /// All executions are logged with structured telemetry including correlation ID,
    /// tool name, attempt count, duration, and outcome. This integrates with OpenTelemetry
    /// for distributed tracing in Aspire deployments.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="toolCall"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the tool name does not exist in the registry or if the browser agent
    /// is not initialized.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="cancellationToken"/>.
    /// </exception>
    Task<ToolExecutionResult> ExecuteToolAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a sequence of browser automation tools in order, stopping on the first failure
    /// unless fallback is configured.
    /// </summary>
    /// <param name="toolCalls">
    /// An ordered collection of tool calls to execute sequentially. Each tool must exist
    /// in the <see cref="BrowserToolRegistry"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation. Cancels any in-progress tool execution
    /// and prevents subsequent tools from starting.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of
    /// <see cref="ToolExecutionResult"/> objects, one for each tool call, in execution order.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Sequential execution guarantees:
    /// - Tools execute in the exact order specified
    /// - Each tool waits for the previous tool to complete
    /// - On failure, execution stops unless recovery is enabled
    /// - Cancellation is checked between each tool execution
    /// - Browser state persists between tool executions
    /// </para>
    /// <para>
    /// This method is suitable for automation workflows where each step depends on the
    /// previous step's outcome (e.g., login ? navigate ? click ? verify).
    /// </para>
    /// <para>
    /// For independent tools that can run in parallel, consider implementing parallel
    /// execution with proper synchronization to avoid browser state conflicts.
    /// </para>
    /// <para>
    /// The <see cref="Options.ToolExecutorOptions.MaxConcurrentTools"/> setting is not
    /// applied to this method as tools execute strictly sequentially.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="toolCalls"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="toolCalls"/> is empty or contains null elements.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="cancellationToken"/>.
    /// </exception>
    Task<IReadOnlyList<ToolExecutionResult>> ExecuteSequenceAsync(
        IEnumerable<ToolCall> toolCalls,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a tool with automatic fallback to alternative implementations or recovery strategies
    /// when the primary execution fails.
    /// </summary>
    /// <param name="toolCall">
    /// The primary tool call to execute. If this fails after retries, fallback strategies
    /// will be attempted.
    /// </param>
    /// <param name="fallbackStrategies">
    /// An ordered list of fallback tool calls to try if the primary execution fails.
    /// Each fallback is attempted in order until one succeeds or all fail.
    /// Can be null or empty to disable fallback behavior.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the operation. Cancels both primary and fallback executions.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="ToolExecutionResult"/> from either the primary execution or the first
    /// successful fallback. The <see cref="ToolExecutionResult.Metadata"/> dictionary
    /// includes a "fallback_used" key indicating whether a fallback was used, and
    /// "primary_error" if a fallback succeeded after primary failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Execution flow:
    /// 1. Execute primary tool with configured retries
    /// 2. If primary fails and fallbacks exist, try each fallback in order
    /// 3. Each fallback gets full retry attempts
    /// 4. Return first successful result or final failure
    /// </para>
    /// <para>
    /// Fallback strategies are useful for:
    /// - Alternative element selectors (CSS ? XPath ? text content)
    /// - Different interaction methods (click ? JavaScript click ? keyboard navigation)
    /// - Simplified approaches (specific selector ? general selector)
    /// - Recovery actions (dismiss popup ? retry original action)
    /// </para>
    /// <para>
    /// Example fallback chain:
    /// <code>
    /// var primary = new ToolCall("click", new { selector = "#submit-btn" }, ...);
    /// var fallbacks = new[]
    /// {
    ///     new ToolCall("click", new { selector = "button[type='submit']" }, ...),
    ///     new ToolCall("click", new { selector = "text='Submit'" }, ...),
    ///     new ToolCall("type", new { selector = "body", text = "{Enter}" }, ...)
    /// };
    /// 
    /// var result = await executor.ExecuteWithFallbackAsync(primary, fallbacks, ct);
    /// </code>
    /// </para>
    /// <para>
    /// All fallback attempts are logged with correlation to the primary execution for
    /// debugging and analysis. The final result includes telemetry about which strategy
    /// succeeded and how many fallbacks were attempted.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="toolCall"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no tools (primary or fallbacks) can be executed successfully.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="cancellationToken"/>.
    /// </exception>
    Task<ToolExecutionResult> ExecuteWithFallbackAsync(
        ToolCall toolCall,
        IEnumerable<ToolCall>? fallbackStrategies = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a tool call without executing it, checking that the tool exists in the registry
    /// and all required parameters are provided with correct types.
    /// </summary>
    /// <param name="toolCall">
    /// The tool call to validate against the <see cref="BrowserToolRegistry"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous validation operation. The task result is true
    /// if the tool call is valid and can be executed; otherwise, false.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Validation checks:
    /// - Tool name exists in the registry (case-insensitive)
    /// - All required parameters are present in the tool call
    /// - Parameter types match the tool definition
    /// - Parameter values are within acceptable ranges (where applicable)
    /// </para>
    /// <para>
    /// This method does not check runtime conditions like browser state or element availability.
    /// It only validates the tool call structure against the registry definition.
    /// </para>
    /// <para>
    /// Use this method to validate tool calls from LLM responses before attempting execution,
    /// or to implement pre-flight validation in automation planning phases.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="toolCall"/> is null.
    /// </exception>
    Task<bool> ValidateToolCallAsync(ToolCall toolCall);

    /// <summary>
    /// Gets the execution history for a specific correlation ID, useful for debugging and analysis.
    /// </summary>
    /// <param name="correlationId">
    /// The correlation ID used to track related tool executions across a workflow or task.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only
    /// list of all <see cref="ToolExecutionResult"/> objects associated with the correlation ID,
    /// ordered by execution time. Returns an empty list if no executions match the correlation ID.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The execution history includes all attempts, retries, and fallbacks associated with
    /// the correlation ID. This is valuable for:
    /// - Debugging failed automation workflows
    /// - Analyzing retry patterns and success rates
    /// - Generating execution reports and statistics
    /// - Training self-healing agents with historical data
    /// </para>
    /// <para>
    /// History is stored in-memory for the lifetime of the executor instance (scoped to the
    /// HTTP request or task execution in Aspire). For persistent history, implement a
    /// separate history service with database backing.
    /// </para>
    /// <para>
    /// The history size is limited by <see cref="Options.ToolExecutorOptions"/> to prevent
    /// memory issues with long-running executors.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="correlationId"/> is null or empty.
    /// </exception>
    Task<IReadOnlyList<ToolExecutionResult>> GetExecutionHistoryAsync(string correlationId);
}
