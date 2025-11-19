using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvoAITest.Core.Services;

/// <summary>
/// Default implementation of <see cref="IToolExecutor"/> that executes browser automation tools
/// with robust retry logic, exponential backoff with jitter, comprehensive error handling, and telemetry.
/// </summary>
/// <remarks>
/// <para>
/// This executor coordinates tool execution through the <see cref="IBrowserAgent"/> interface,
/// validates tools against the <see cref="IBrowserToolRegistry"/>, and provides production-ready
/// resilience patterns including:
/// - Exponential backoff with jitter (following Microsoft's retry guidance)
/// - Transient error detection and retry
/// - Terminal error fast-fail
/// - Cancellation support at every retry boundary
/// - OpenTelemetry-compatible distributed tracing
/// - High-performance structured logging with LoggerMessage source generators
/// - Metrics for observability (executions, duration, active operations)
/// - In-memory execution history per correlation ID
/// </para>
/// <para>
/// The executor is designed as a scoped service, maintaining execution history for the lifetime
/// of a single automation workflow or HTTP request in Aspire deployments.
/// </para>
/// </remarks>
public sealed class DefaultToolExecutor : IToolExecutor
{
    private readonly IBrowserAgent _browserAgent;
    private readonly IBrowserToolRegistry _toolRegistry;
    private readonly ToolExecutorOptions _options;
    private readonly ILogger<DefaultToolExecutor> _logger;
    
    // OpenTelemetry tracing
    private static readonly ActivitySource ActivitySource = new("EvoAITest.ToolExecutor", "1.0.0");
    
    // Metrics
    private static readonly Meter Meter = new("EvoAITest.ToolExecutor", "1.0.0");
    private static readonly Counter<long> ToolExecutionsTotal = Meter.CreateCounter<long>(
        "tool_executions_total",
        description: "Total number of tool executions");
    private static readonly Histogram<double> ToolExecutionDuration = Meter.CreateHistogram<double>(
        "tool_execution_duration_ms",
        unit: "ms",
        description: "Duration of tool executions in milliseconds");
    private static readonly UpDownCounter<int> ActiveToolExecutions = Meter.CreateUpDownCounter<int>(
        "active_tool_executions",
        description: "Number of currently executing tools");
    
    // In-memory execution history keyed by correlation ID
    private readonly ConcurrentDictionary<string, List<ToolExecutionResult>> _executionHistory = new();
    private readonly Random _jitterRandom = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultToolExecutor"/> class.
    /// </summary>
    /// <param name="browserAgent">The browser agent for executing browser operations.</param>
    /// <param name="toolRegistry">The tool registry for validation and metadata.</param>
    /// <param name="options">Configuration options for retry behavior and timeouts.</param>
    /// <param name="logger">Logger for structured telemetry.</param>
    public DefaultToolExecutor(
        IBrowserAgent browserAgent,
        IBrowserToolRegistry toolRegistry,
        IOptions<ToolExecutorOptions> options,
        ILogger<DefaultToolExecutor> logger)
    {
        _browserAgent = browserAgent ?? throw new ArgumentNullException(nameof(browserAgent));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // Validate options on construction
        _options.Validate();
        
        // Log initialization
        if (_options.EnableDetailedLogging)
        {
            _logger.ExecutorInitialized(
                _options.MaxRetries,
                _options.InitialRetryDelayMs,
                _options.MaxRetryDelayMs,
                _options.TimeoutPerToolMs);
            
            if (_options.UseExponentialBackoff)
            {
                _logger.ExponentialBackoffEnabled(
                    _options.InitialRetryDelayMs,
                    _options.MaxRetryDelayMs);
            }
        }
    }

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteToolAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        
        // Start distributed tracing activity
        using var activity = ActivitySource.StartActivity("ExecuteTool", ActivityKind.Internal);
        activity?.SetTag("tool.name", toolCall.ToolName);
        activity?.SetTag("tool.correlation_id", toolCall.CorrelationId);
        activity?.SetTag("tool.reasoning", toolCall.Reasoning);
        
        // Increment active executions
        ActiveToolExecutions.Add(1);
        
        var stopwatch = Stopwatch.StartNew();
        var attemptCount = 0;
        Exception? lastError = null;
        var retryReasons = new List<string>();
        var retryDelays = new List<int>();

        try
        {
            // Validate tool exists in registry
            if (!_toolRegistry.ToolExists(toolCall.ToolName))
            {
                var availableTools = string.Join(", ", _toolRegistry.GetToolNames());
                _logger.ToolNotFound(toolCall.ToolName, availableTools);
                
                activity?.SetStatus(ActivityStatusCode.Error, "Tool not found");
                activity?.SetTag("error.type", "tool_not_found");
                
                var error = new InvalidOperationException(
                    $"Tool '{toolCall.ToolName}' not found in registry. " +
                    $"Available tools: {availableTools}");
                
                var failureResult = ToolExecutionResult.Failed(
                    toolCall.ToolName,
                    error,
                    stopwatch.Elapsed,
                    1,
                    new Dictionary<string, object>
                    {
                        ["correlation_id"] = toolCall.CorrelationId,
                        ["validation_error"] = "tool_not_found"
                    });
                
                RecordMetrics(toolCall.ToolName, failureResult, stopwatch.Elapsed);
                AddToHistory(toolCall.CorrelationId, failureResult);
                return failureResult;
            }

            // Validate required parameters
            var validationError = ValidateParameters(toolCall);
            if (validationError != null)
            {
                _logger.ParameterValidationFailed(toolCall.ToolName, validationError);
                
                activity?.SetStatus(ActivityStatusCode.Error, "Parameter validation failed");
                activity?.SetTag("error.type", "parameter_validation_failed");
                activity?.SetTag("error.message", validationError);
                
                var failureResult = ToolExecutionResult.Failed(
                    toolCall.ToolName,
                    new ArgumentException(validationError),
                    stopwatch.Elapsed,
                    1,
                    new Dictionary<string, object>
                    {
                        ["correlation_id"] = toolCall.CorrelationId,
                        ["validation_error"] = "missing_required_parameters"
                    });
                
                RecordMetrics(toolCall.ToolName, failureResult, stopwatch.Elapsed);
                AddToHistory(toolCall.CorrelationId, failureResult);
                return failureResult;
            }

            // Execute with retry logic
            var maxAttempts = _options.MaxRetries + 1; // Initial attempt + retries
            activity?.SetTag("tool.max_attempts", maxAttempts);
            
            for (attemptCount = 1; attemptCount <= maxAttempts; attemptCount++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Log execution attempt
                if (_options.EnableDetailedLogging)
                {
                    _logger.ExecutingTool(toolCall.ToolName, attemptCount, maxAttempts, toolCall.CorrelationId);
                }
                
                activity?.SetTag("tool.current_attempt", attemptCount);

                try
                {
                    // Create timeout for this attempt
                    using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    attemptCts.CancelAfter(TimeSpan.FromMilliseconds(_options.TimeoutPerToolMs));

                    // Execute the tool
                    var executionStopwatch = Stopwatch.StartNew();
                    var result = await ExecuteToolInternalAsync(toolCall, attemptCts.Token).ConfigureAwait(false);
                    executionStopwatch.Stop();

                    // Success - create result with metadata
                    var metadata = new Dictionary<string, object>
                    {
                        ["correlation_id"] = toolCall.CorrelationId,
                        ["reasoning"] = toolCall.Reasoning,
                        ["attempt_count"] = attemptCount
                    };

                    if (retryReasons.Count > 0)
                    {
                        metadata["retry_reasons"] = retryReasons.ToArray();
                        metadata["retry_delays"] = retryDelays.ToArray();
                    }

                    var successResult = ToolExecutionResult.Succeeded(
                        toolCall.ToolName,
                        result,
                        stopwatch.Elapsed,
                        attemptCount,
                        metadata);

                    // Log success
                    if (_options.EnableDetailedLogging)
                    {
                        _logger.ToolExecutionSucceeded(
                            toolCall.ToolName,
                            stopwatch.ElapsedMilliseconds,
                            attemptCount);
                    }
                    
                    // Set activity tags
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    activity?.SetTag("tool.success", true);
                    activity?.SetTag("tool.attempt_count", attemptCount);
                    activity?.SetTag("tool.duration_ms", stopwatch.ElapsedMilliseconds);
                    
                    if (attemptCount > 1)
                    {
                        activity?.SetTag("tool.retried", true);
                        activity?.SetTag("tool.retry_count", attemptCount - 1);
                    }

                    RecordMetrics(toolCall.ToolName, successResult, stopwatch.Elapsed);
                    AddToHistory(toolCall.CorrelationId, successResult);
                    return successResult;
                }
                catch (Exception ex) when (IsTransientError(ex) && 
                                           attemptCount < maxAttempts && 
                                           !cancellationToken.IsCancellationRequested)
                {
                    lastError = ex;
                    var errorType = ex.GetType().Name;
                    var retryReason = $"{errorType}: {ex.Message}";
                    retryReasons.Add(retryReason);

                    // Log transient error
                    if (_options.EnableDetailedLogging)
                    {
                        _logger.TransientErrorDetected(toolCall.ToolName, errorType, ex.Message);
                    }

                    // Calculate backoff delay with jitter
                    var delay = CalculateBackoffDelay(attemptCount - 1); // 0-indexed for calculation
                    retryDelays.Add(delay);

                    // Log retry
                    if (_options.EnableDetailedLogging)
                    {
                        _logger.RetryingToolExecution(
                            toolCall.ToolName,
                            attemptCount + 1,
                            maxAttempts,
                            delay,
                            retryReason);
                    }
                    
                    // Add retry event to activity
                    var retryEvent = new ActivityEvent(
                        "Retry",
                        tags: new ActivityTagsCollection
                        {
                            { "retry.attempt", attemptCount },
                            { "retry.delay_ms", delay },
                            { "retry.reason", retryReason }
                        });
                    activity?.AddEvent(retryEvent);

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Terminal error or cancellation - don't retry
                    lastError = ex;
                    
                    if (ex is OperationCanceledException)
                    {
                        _logger.ExecutionCanceled(toolCall.ToolName, attemptCount);
                        activity?.SetStatus(ActivityStatusCode.Error, "Canceled");
                        activity?.SetTag("error.type", "canceled");
                        throw;
                    }

                    var errorType = ex.GetType().Name;
                    _logger.TerminalErrorDetected(ex, toolCall.ToolName, errorType);
                    
                    activity?.SetStatus(ActivityStatusCode.Error, "Terminal error");
                    activity?.SetTag("error.type", errorType);
                    activity?.SetTag("error.message", ex.Message);
                    
                    break; // Exit retry loop for terminal errors
                }
            }

            // All attempts failed
            stopwatch.Stop();
            
            var finalMetadata = new Dictionary<string, object>
            {
                ["correlation_id"] = toolCall.CorrelationId,
                ["reasoning"] = toolCall.Reasoning,
                ["attempt_count"] = attemptCount,
                ["retry_reasons"] = retryReasons.ToArray(),
                ["retry_delays"] = retryDelays.ToArray()
            };

            var finalResult = ToolExecutionResult.Failed(
                toolCall.ToolName,
                lastError ?? new InvalidOperationException("Tool execution failed"),
                stopwatch.Elapsed,
                attemptCount,
                finalMetadata);

            _logger.ToolExecutionFailed(
                lastError!,
                toolCall.ToolName,
                attemptCount,
                stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, "All attempts failed");
            activity?.SetTag("tool.success", false);
            activity?.SetTag("tool.attempt_count", attemptCount);
            activity?.SetTag("tool.duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("error.type", lastError?.GetType().Name ?? "unknown");

            RecordMetrics(toolCall.ToolName, finalResult, stopwatch.Elapsed);
            AddToHistory(toolCall.CorrelationId, finalResult);
            return finalResult;
        }
        finally
        {
            // Decrement active executions
            ActiveToolExecutions.Add(-1);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ToolExecutionResult>> ExecuteSequenceAsync(
        IEnumerable<ToolCall> toolCalls,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        var toolCallsList = toolCalls.ToList();
        
        if (toolCallsList.Count == 0)
        {
            throw new ArgumentException("Tool calls sequence cannot be empty.", nameof(toolCalls));
        }

        if (toolCallsList.Any(tc => tc == null))
        {
            throw new ArgumentException("Tool calls sequence contains null elements.", nameof(toolCalls));
        }

        // Start distributed tracing activity for the sequence
        using var activity = ActivitySource.StartActivity("ExecuteToolSequence", ActivityKind.Internal);
        activity?.SetTag("sequence.tool_count", toolCallsList.Count);
        
        // Use first correlation ID for sequence tracking
        var sequenceCorrelationId = toolCallsList[0].CorrelationId;
        activity?.SetTag("sequence.correlation_id", sequenceCorrelationId);
        
        var sequenceStopwatch = Stopwatch.StartNew();
        var results = new List<ToolExecutionResult>();
        
        _logger.StartingSequenceExecution(toolCallsList.Count, sequenceCorrelationId);

        try
        {
            for (int i = 0; i < toolCallsList.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var toolCall = toolCallsList[i];
                
                if (_options.EnableDetailedLogging)
                {
                    _logger.SequenceToolExecution(i + 1, toolCallsList.Count, toolCall.ToolName);
                }

                var result = await ExecuteToolAsync(toolCall, cancellationToken).ConfigureAwait(false);
                results.Add(result);

                // Stop on first failure (fail-fast for sequential execution)
                if (!result.Success)
                {
                    _logger.SequenceExecutionStopped(i + 1, toolCallsList.Count, toolCall.ToolName);
                    
                    activity?.SetStatus(ActivityStatusCode.Error, "Sequence stopped on failure");
                    activity?.SetTag("sequence.failed_at_index", i);
                    activity?.SetTag("sequence.failed_tool", toolCall.ToolName);
                    
                    break;
                }
            }

            sequenceStopwatch.Stop();
            
            var successCount = results.Count(r => r.Success);
            _logger.SequenceExecutionCompleted(
                successCount,
                results.Count,
                sequenceStopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(successCount == results.Count ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            activity?.SetTag("sequence.success_count", successCount);
            activity?.SetTag("sequence.total_count", results.Count);
            activity?.SetTag("sequence.duration_ms", sequenceStopwatch.ElapsedMilliseconds);
            activity?.SetTag("sequence.success", successCount == results.Count);

            return results.AsReadOnly();
        }
        catch (OperationCanceledException)
        {
            var lastIndex = results.Count;
            var lastTool = lastIndex < toolCallsList.Count ? toolCallsList[lastIndex].ToolName : "unknown";
            
            _logger.SequenceCanceled(lastIndex + 1, toolCallsList.Count, lastTool);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Sequence canceled");
            activity?.SetTag("sequence.canceled_at_index", lastIndex);
            
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ToolExecutionResult> ExecuteWithFallbackAsync(
        ToolCall toolCall,
        IEnumerable<ToolCall>? fallbackStrategies = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        // Start distributed tracing activity for fallback execution
        using var activity = ActivitySource.StartActivity("ExecuteToolWithFallback", ActivityKind.Internal);
        activity?.SetTag("tool.name", toolCall.ToolName);
        activity?.SetTag("tool.correlation_id", toolCall.CorrelationId);

        // Execute primary tool (creates child activity via ExecuteToolAsync)
        var primaryResult = await ExecuteToolAsync(toolCall, cancellationToken).ConfigureAwait(false);

        // If primary succeeded or no fallbacks, return primary result
        if (primaryResult.Success || fallbackStrategies == null)
        {
            activity?.SetTag("fallback.used", false);
            activity?.SetTag("fallback.primary_success", primaryResult.Success);
            return primaryResult;
        }

        var fallbackList = fallbackStrategies.ToList();
        if (fallbackList.Count == 0)
        {
            activity?.SetTag("fallback.used", false);
            return primaryResult;
        }

        activity?.SetTag("fallback.count", fallbackList.Count);
        _logger.StartingFallbackExecution(toolCall.ToolName, fallbackList.Count);

        // Store primary error for metadata
        var primaryError = primaryResult.Error?.Message ?? "Unknown error";

        // Try each fallback in order
        for (int i = 0; i < fallbackList.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fallback = fallbackList[i];
            
            if (_options.EnableDetailedLogging)
            {
                _logger.TryingFallback(i + 1, fallbackList.Count, toolCall.ToolName, fallback.ToolName);
            }

            var fallbackResult = await ExecuteToolAsync(fallback, cancellationToken).ConfigureAwait(false);

            if (fallbackResult.Success)
            {
                // Fallback succeeded - enrich metadata
                var enrichedResult = fallbackResult
                    .WithMetadata("fallback_used", true)
                    .WithMetadata("fallback_index", i)
                    .WithMetadata("primary_tool", toolCall.ToolName)
                    .WithMetadata("primary_error", primaryError);

                _logger.FallbackSucceeded(i + 1, fallbackList.Count, toolCall.ToolName, fallback.ToolName);
                
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("fallback.used", true);
                activity?.SetTag("fallback.success_index", i);
                activity?.SetTag("fallback.success_tool", fallback.ToolName);

                return enrichedResult;
            }
        }

        // All fallbacks failed - return primary result with fallback metadata
        _logger.AllFallbacksFailed(fallbackList.Count, toolCall.ToolName);
        
        activity?.SetStatus(ActivityStatusCode.Error, "All fallbacks failed");
        activity?.SetTag("fallback.used", true);
        activity?.SetTag("fallback.all_failed", true);

        return primaryResult.WithMetadata(new Dictionary<string, object>
        {
            ["fallback_attempted"] = true,
            ["fallback_count"] = fallbackList.Count,
            ["all_fallbacks_failed"] = true
        });
    }

    /// <inheritdoc />
    public Task<bool> ValidateToolCallAsync(ToolCall toolCall)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        // Check if tool exists
        if (!_toolRegistry.ToolExists(toolCall.ToolName))
        {
            return Task.FromResult(false);
        }

        // Validate parameters
        var validationError = ValidateParameters(toolCall);
        return Task.FromResult(validationError == null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ToolExecutionResult>> GetExecutionHistoryAsync(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        if (_executionHistory.TryGetValue(correlationId, out var history))
        {
            if (_options.EnableDetailedLogging)
            {
                _logger.RetrievingHistory(correlationId, history.Count);
            }
            
            return Task.FromResult<IReadOnlyList<ToolExecutionResult>>(history.AsReadOnly());
        }

        if (_options.EnableDetailedLogging)
        {
            _logger.RetrievingHistory(correlationId, 0);
        }
        
        return Task.FromResult<IReadOnlyList<ToolExecutionResult>>(Array.Empty<ToolExecutionResult>());
    }

    /// <summary>
    /// Executes a tool by dispatching to the appropriate <see cref="IBrowserAgent"/> method.
    /// </summary>
    private async Task<object?> ExecuteToolInternalAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (_options.EnableDetailedLogging)
        {
            _logger.DispatchingToBrowserAgent(toolCall.ToolName, toolCall.Parameters.Count);
        }
        
        var dispatchStopwatch = Stopwatch.StartNew();
        
        try
        {
            // Dispatch to appropriate browser agent method based on tool name
            var result = toolCall.ToolName.ToLowerInvariant() switch
            {
                "navigate" => await ExecuteNavigateAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "click" => await ExecuteClickAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "type" => await ExecuteTypeAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "get_text" or "extract_text" => await ExecuteGetTextAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "take_screenshot" => await ExecuteTakeScreenshotAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "wait_for_element" => await ExecuteWaitForElementAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "get_page_state" => await ExecuteGetPageStateAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "get_page_html" => await ExecuteGetPageHtmlAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "clear_input" => await ExecuteClearInputAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "extract_table" => throw new NotImplementedException($"Tool '{toolCall.ToolName}' is not yet implemented"),
                "wait_for_url_change" => throw new NotImplementedException($"Tool '{toolCall.ToolName}' is not yet implemented"),
                "select_option" => throw new NotImplementedException($"Tool '{toolCall.ToolName}' is not yet implemented"),
                "submit_form" => throw new NotImplementedException($"Tool '{toolCall.ToolName}' is not yet implemented"),
                "verify_element_exists" => throw new NotImplementedException($"Tool '{toolCall.ToolName}' is not yet implemented"),
                _ => throw new InvalidOperationException($"Unknown tool: {toolCall.ToolName}")
            };
            
            dispatchStopwatch.Stop();
            
            if (_options.EnableDetailedLogging)
            {
                _logger.BrowserAgentOperationCompleted(toolCall.ToolName, dispatchStopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (TimeoutException)
        {
            if (_options.EnableDetailedLogging)
            {
                _logger.BrowserAgentTimeout(toolCall.ToolName, _options.TimeoutPerToolMs);
            }
            throw;
        }
    }

    #region Tool Implementation Methods

    private async Task<object?> ExecuteNavigateAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var url = GetRequiredParameter<string>(toolCall, "url");
        await _browserAgent.NavigateAsync(url, cancellationToken).ConfigureAwait(false);
        return null;
    }

    private async Task<object?> ExecuteClickAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var selector = GetRequiredParameter<string>(toolCall, "selector");
        var maxRetries = GetOptionalParameter(toolCall, "maxRetries", 3);
        await _browserAgent.ClickAsync(selector, maxRetries, cancellationToken).ConfigureAwait(false);
        return null;
    }

    private async Task<object?> ExecuteTypeAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var selector = GetRequiredParameter<string>(toolCall, "selector");
        var text = GetRequiredParameter<string>(toolCall, "text");
        await _browserAgent.TypeAsync(selector, text, cancellationToken).ConfigureAwait(false);
        return null;
    }

    private async Task<object?> ExecuteGetTextAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var selector = GetRequiredParameter<string>(toolCall, "selector");
        var text = await _browserAgent.GetTextAsync(selector, cancellationToken).ConfigureAwait(false);
        return text;
    }

    private async Task<object?> ExecuteTakeScreenshotAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var screenshot = await _browserAgent.TakeScreenshotAsync(cancellationToken).ConfigureAwait(false);
        return screenshot;
    }

    private async Task<object?> ExecuteWaitForElementAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var selector = GetRequiredParameter<string>(toolCall, "selector");
        var timeoutMs = GetOptionalParameter(toolCall, "timeout_ms", _options.TimeoutPerToolMs);
        await _browserAgent.WaitForElementAsync(selector, timeoutMs, cancellationToken).ConfigureAwait(false);
        return null;
    }

    private async Task<object?> ExecuteGetPageStateAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var pageState = await _browserAgent.GetPageStateAsync(cancellationToken).ConfigureAwait(false);
        return pageState;
    }

    private async Task<object?> ExecuteGetPageHtmlAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var html = await _browserAgent.GetPageHtmlAsync(cancellationToken).ConfigureAwait(false);
        return html;
    }

    private async Task<object?> ExecuteClearInputAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        // Clear input by typing empty string
        var selector = GetRequiredParameter<string>(toolCall, "selector");
        await _browserAgent.TypeAsync(selector, string.Empty, cancellationToken).ConfigureAwait(false);
        return null;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Validates that all required parameters for a tool are present and valid.
    /// </summary>
    /// <returns>Error message if validation fails; otherwise, null.</returns>
    private string? ValidateParameters(ToolCall toolCall)
    {
        var toolDef = _toolRegistry.GetTool(toolCall.ToolName);
        var missingParams = new List<string>();

        foreach (var (paramName, paramDef) in toolDef.Parameters)
        {
            if (paramDef.Required && !toolCall.Parameters.ContainsKey(paramName))
            {
                missingParams.Add(paramName);
            }
        }

        if (missingParams.Count > 0)
        {
            return $"Missing required parameters for tool '{toolCall.ToolName}': {string.Join(", ", missingParams)}";
        }

        return null;
    }

    /// <summary>
    /// Gets a required parameter from the tool call, throwing if not found.
    /// </summary>
    private T GetRequiredParameter<T>(ToolCall toolCall, string parameterName)
    {
        if (!toolCall.Parameters.TryGetValue(parameterName, out var value))
        {
            throw new ArgumentException(
                $"Required parameter '{parameterName}' not found in tool call '{toolCall.ToolName}'");
        }

        try
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Try to convert via JSON serialization for complex types
            var json = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize<T>(json) 
                ?? throw new InvalidCastException($"Failed to deserialize parameter '{parameterName}' to type {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"Parameter '{parameterName}' in tool call '{toolCall.ToolName}' " +
                $"cannot be converted to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Gets an optional parameter from the tool call, returning default value if not found.
    /// </summary>
    private T GetOptionalParameter<T>(ToolCall toolCall, string parameterName, T defaultValue)
    {
        if (!toolCall.Parameters.TryGetValue(parameterName, out var value))
        {
            return defaultValue;
        }

        try
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Handle numeric conversions (e.g., int to long, double to int)
            if (typeof(T).IsPrimitive && value != null)
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }

            var json = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize<T>(json) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Determines if an exception represents a transient error that should be retried.
    /// </summary>
    private bool IsTransientError(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            OperationCanceledException => false, // Don't retry cancellation
            ArgumentNullException => false, // Terminal - invalid arguments (checked before ArgumentException)
            ArgumentException => false, // Terminal - invalid arguments
            InvalidOperationException => false, // Terminal - invalid operation
            NotImplementedException => false, // Terminal - not implemented
            _ => IsPlaywrightTransientError(exception)
        };
    }

    /// <summary>
    /// Checks if a Playwright exception is transient (element not found, stale reference, etc.).
    /// </summary>
    private bool IsPlaywrightTransientError(Exception exception)
    {
        var exceptionType = exception.GetType().Name;
        var message = exception.Message ?? string.Empty;

        // Playwright-specific transient errors
        return exceptionType == "PlaywrightException" &&
               (message.Contains("Timeout") ||
                message.Contains("Element is not attached") ||
                message.Contains("Element is not visible") ||
                message.Contains("ElementNotInteractableException") ||
                message.Contains("StaleElementReferenceException") ||
                message.Contains("Element not found"));
    }

    /// <summary>
    /// Calculates the backoff delay for a retry attempt with exponential backoff and jitter.
    /// </summary>
    /// <param name="retryAttempt">The retry attempt number (0-indexed).</param>
    /// <returns>Delay in milliseconds before the next retry.</returns>
    private int CalculateBackoffDelay(int retryAttempt)
    {
        if (!_options.UseExponentialBackoff)
        {
            // Fixed delay
            return _options.InitialRetryDelayMs;
        }

        // Exponential backoff: delay = initialDelay * 2^attempt
        var exponentialDelay = _options.InitialRetryDelayMs * Math.Pow(2, retryAttempt);
        
        // Cap at maximum delay
        var cappedDelay = Math.Min(exponentialDelay, _options.MaxRetryDelayMs);
        
        // Add jitter (±25%) to prevent thundering herd
        var jitterRange = cappedDelay * 0.25;
        var jitter = (_jitterRandom.NextDouble() * 2 - 1) * jitterRange; // Random between -25% and +25%
        var finalDelay = cappedDelay + jitter;
        
        var delayMs = (int)Math.Max(finalDelay, _options.InitialRetryDelayMs);
        
        if (_options.EnableDetailedLogging)
        {
            _logger.BackoffCalculated(retryAttempt + 1, delayMs);
        }
        
        return delayMs;
    }

    /// <summary>
    /// Adds an execution result to the in-memory history for a correlation ID.
    /// </summary>
    private void AddToHistory(string correlationId, ToolExecutionResult result)
    {
        if (_options.EnableDetailedLogging)
        {
            _logger.AddingToHistory(correlationId, result.ToolName, result.Success);
        }
        
        var history = _executionHistory.GetOrAdd(correlationId, _ => new List<ToolExecutionResult>());
        
        lock (history)
        {
            history.Add(result);
            
            // Trim history if it exceeds maximum size
            if (history.Count > _options.MaxHistorySize)
            {
                history.RemoveAt(0); // Remove oldest entry (FIFO)
                
                if (_options.EnableDetailedLogging)
                {
                    _logger.HistoryTrimmed(correlationId, _options.MaxHistorySize);
                }
            }
        }
    }
    
    /// <summary>
    /// Records OpenTelemetry metrics for tool execution.
    /// </summary>
    private void RecordMetrics(string toolName, ToolExecutionResult result, TimeSpan duration)
    {
        var status = result.Success ? "success" : "failure";
        var tags = new TagList
        {
            { "tool.name", toolName },
            { "tool.status", status }
        };
        
        // Record execution count
        ToolExecutionsTotal.Add(1, tags);
        
        // Record execution duration
        ToolExecutionDuration.Record(duration.TotalMilliseconds, tags);
    }

    #endregion
}
