using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Browser;
using EvoAITest.Core.Models;
using EvoAITest.Core.Options;
using EvoAITest.Core.Services.ErrorRecovery;
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
    private readonly IVisualComparisonService? _visualComparisonService;
    private readonly IAccessibilityService? _accessibilityService;
    private readonly ISelectorHealingService? _selectorHealingService;
    private readonly IErrorRecoveryService? _errorRecoveryService;
    
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
    
    // Selector error detection patterns
    private static readonly string[] SelectorErrorPatterns =
    {
        "css selector",
        "xpath",
        "no such element",
        "unable to locate element",
        "element not found",
        "element is not attached to the page document",
        "timeout waiting for selector",
        "timeout waiting for element",
        "waiting for selector",
        "waiting for element"
    };
    
    // Compiled regexes for selector text extraction
    private static readonly System.Text.RegularExpressions.Regex ContainsRegex = 
        new(@":contains\([""']([^""']+)[""']\)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | 
            System.Text.RegularExpressions.RegexOptions.Compiled);
    
    private static readonly System.Text.RegularExpressions.Regex HasTextRegex = 
        new(@":has-text\([""']([^""']+)[""']\)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | 
            System.Text.RegularExpressions.RegexOptions.Compiled);
    
    private static readonly System.Text.RegularExpressions.Regex TextAttrRegex = 
        new(@"\[text=[""']([^""']+)[""']\]", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | 
            System.Text.RegularExpressions.RegexOptions.Compiled);
    
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
    /// <param name="visualComparisonService">Optional service for visual regression testing.</param>
    /// <param name="accessibilityService">Optional service for accessibility checks.</param>
    /// <param name="selectorHealingService">Optional service for automatic selector healing.</param>
    /// <param name="errorRecoveryService">Optional service for intelligent error recovery.</param>
    public DefaultToolExecutor(
        IBrowserAgent browserAgent,
        IBrowserToolRegistry toolRegistry,
        IOptions<ToolExecutorOptions> options,
        ILogger<DefaultToolExecutor> logger,
        IVisualComparisonService? visualComparisonService = null,
        IAccessibilityService? accessibilityService = null,
        ISelectorHealingService? selectorHealingService = null,
        IErrorRecoveryService? errorRecoveryService = null)
    {
        _browserAgent = browserAgent ?? throw new ArgumentNullException(nameof(browserAgent));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _visualComparisonService = visualComparisonService; // Optional for backwards compatibility
        _accessibilityService = accessibilityService;
        _selectorHealingService = selectorHealingService; // Optional for self-healing capability
        _errorRecoveryService = errorRecoveryService; // Optional for intelligent error recovery
        
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

                    // Try intelligent error recovery if service is available
                    if (_errorRecoveryService != null)
                    {
                        try
                        {
                            var pageState = await _browserAgent.GetPageStateAsync(cancellationToken);
                            var executionContext = new Models.ExecutionContext
                            {
                                Action = toolCall.ToolName,
                                Selector = toolCall.Parameters.TryGetValue("selector", out var sel) 
                                    ? sel?.ToString() : null,
                                PageUrl = pageState.Url,
                                ExpectedText = toolCall.Parameters.TryGetValue("text", out var txt) 
                                    ? txt?.ToString() : null
                            };

                            var recoveryStrategy = new Models.ErrorRecovery.RetryStrategy
                            {
                                MaxRetries = maxAttempts - attemptCount,
                                InitialDelay = TimeSpan.FromMilliseconds(_options.InitialRetryDelayMs),
                                MaxDelay = TimeSpan.FromMilliseconds(_options.MaxRetryDelayMs),
                                UseExponentialBackoff = _options.UseExponentialBackoff,
                                UseJitter = true
                            };

                            var recoveryResult = await _errorRecoveryService.RecoverAsync(
                                ex,
                                executionContext,
                                recoveryStrategy,
                                cancellationToken);

                            if (recoveryResult.Success)
                            {
                                _logger.LogInformation(
                                    "Error recovery successful for {Tool} using actions: {Actions}",
                                    toolCall.ToolName,
                                    string.Join(", ", recoveryResult.ActionsAttempted));

                                // Update metadata with recovery info
                                retryReasons.Add($"Recovery: {string.Join(", ", recoveryResult.ActionsAttempted)}");
                                
                                // Recovery handled the error, continue to retry
                                continue;
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Error recovery failed for {Tool} after {Attempts} attempts",
                                    toolCall.ToolName,
                                    recoveryResult.AttemptNumber);
                            }
                        }
                        catch (Exception recoveryEx)
                        {
                            _logger.LogWarning(
                                recoveryEx,
                                "Error recovery service failed for {Tool}",
                                toolCall.ToolName);
                        }
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
                "visual_check" => await ExecuteVisualCheckAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "accessibility_check" => await ExecuteAccessibilityCheckAsync(toolCall, cancellationToken).ConfigureAwait(false),
                
                // Mobile Device Emulation Tools
                "set_device_emulation" => await ExecuteSetDeviceEmulationAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "set_geolocation" => await ExecuteSetGeolocationAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "set_timezone" => await ExecuteSetTimezoneAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "set_locale" => await ExecuteSetLocaleAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "grant_permissions" => await ExecuteGrantPermissionsAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "clear_permissions" => await ExecuteClearPermissionsAsync(toolCall, cancellationToken).ConfigureAwait(false),
                
                // Network Interception Tools
                "mock_response" => await ExecuteMockResponseAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "block_request" => await ExecuteBlockRequestAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "intercept_request" => await ExecuteInterceptRequestAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "get_network_logs" => await ExecuteGetNetworkLogsAsync(toolCall, cancellationToken).ConfigureAwait(false),
                "clear_interceptions" => await ExecuteClearInterceptionsAsync(toolCall, cancellationToken).ConfigureAwait(false),
                
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
        
        try
        {
            await _browserAgent.ClickAsync(selector, maxRetries, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex) when (_selectorHealingService != null && IsSelectorError(ex))
        {
            // Attempt automatic healing
            _logger.LogInformation("Attempting automatic selector healing for failed selector: {Selector}", selector);
            
            // Try to extract expected text from the selector if it contains text-based selectors
            string? expectedText = ExtractExpectedTextFromSelector(selector);
            
            var healedSelector = await TryHealSelectorAsync(selector, expectedText, cancellationToken).ConfigureAwait(false);
            
            if (healedSelector != null)
            {
                // Retry with healed selector
                _logger.LogInformation(
                    "Healed selector found with {Strategy} strategy (confidence: {Confidence:F2}). Retrying click...",
                    healedSelector.Strategy, healedSelector.ConfidenceScore);
                
                await _browserAgent.ClickAsync(healedSelector.NewSelector, maxRetries, cancellationToken).ConfigureAwait(false);
                
                // Log successful healing
                await LogSuccessfulHealingAsync(selector, healedSelector, cancellationToken).ConfigureAwait(false);
                
                return null;
            }
            
            // Healing failed, re-throw original exception
            _logger.LogWarning("Selector healing failed for: {Selector}", selector);
            throw;
        }
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

    private async Task<object?> ExecuteAccessibilityCheckAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        // Parse optional parameters
        var tagsParam = GetOptionalParameter<object?>(toolCall, "tags", null);
        var saveReport = GetOptionalParameter(toolCall, "save_report", true);
        
        List<string>? tags = null;
        if (tagsParam != null)
        {
            if (tagsParam is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                tags = jsonElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
            else if (tagsParam is IEnumerable<object> objEnum)
            {
                tags = objEnum.Select(o => o.ToString()!).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
        }

        var report = await _browserAgent.RunAccessibilityAuditAsync(tags, cancellationToken).ConfigureAwait(false);

        if (saveReport && _accessibilityService != null)
        {
            // Extract IDs from parameters if available
            if (toolCall.Parameters.TryGetValue("task_id", out var taskIdObj) && 
                (taskIdObj is Guid taskId || (taskIdObj is string s && Guid.TryParse(s, out taskId))))
            {
                report.AutomationTaskId = taskId;
            }
            if (toolCall.Parameters.TryGetValue("execution_history_id", out var ehIdObj) && 
                (ehIdObj is Guid ehId || (ehIdObj is string s2 && Guid.TryParse(s2, out ehId))))
            {
                report.ExecutionHistoryId = ehId;
            }

            // Save report
            await _accessibilityService.SaveReportAsync(report, cancellationToken).ConfigureAwait(false);
        }

        return new Dictionary<string, object>
        {
            ["score"] = report.Score,
            ["violations_count"] = report.ViolationCount,
            ["critical_violations"] = report.CriticalCount,
            ["report_id"] = report.Id
        };
    }

    private async Task<object?> ExecuteVisualCheckAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        if (_visualComparisonService == null)
        {
            throw new InvalidOperationException(
                "Visual regression testing is not configured. " +
                "Ensure IVisualComparisonService is registered in the DI container.");
        }

        // Parse required parameters
        var checkpointName = GetRequiredParameter<string>(toolCall, "checkpoint_name");
        var checkpointTypeStr = GetRequiredParameter<string>(toolCall, "checkpoint_type");
        
        // Parse checkpoint type
        if (!Enum.TryParse<CheckpointType>(checkpointTypeStr, true, out var checkpointType))
        {
            throw new ArgumentException(
                $"Invalid checkpoint_type '{checkpointTypeStr}'. " +
                $"Valid values are: {string.Join(", ", Enum.GetNames(typeof(CheckpointType)))}");
        }

        // Parse optional parameters
        var tolerance = GetOptionalParameter(toolCall, "tolerance", 0.01);
        var selector = GetOptionalParameter<string?>(toolCall, "selector", null);
        var ignoreSelectorsParam = GetOptionalParameter<object?>(toolCall, "ignore_selectors", null);
        
        // Parse ignore selectors
        var ignoreSelectors = new List<string>();
        if (ignoreSelectorsParam != null)
        {
            if (ignoreSelectorsParam is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                ignoreSelectors = jsonElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
            else if (ignoreSelectorsParam is string[] stringArray)
            {
                ignoreSelectors = stringArray.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
        }

        // Parse region for region type
        ScreenshotRegion? region = null;
        if (checkpointType == CheckpointType.Region)
        {
            var regionParam = GetOptionalParameter<object?>(toolCall, "region", null);
            if (regionParam == null)
            {
                throw new ArgumentException("Parameter 'region' is required for checkpoint_type 'region'");
            }

            // Try to parse region from JSON
            try
            {
                var regionJson = JsonSerializer.Serialize(regionParam);
                region = JsonSerializer.Deserialize<ScreenshotRegion>(regionJson);
                
                if (region == null)
                {
                    throw new ArgumentException("Failed to parse 'region' parameter");
                }
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid 'region' parameter: {ex.Message}", ex);
            }
        }

        // Validate selector for element type
        if (checkpointType == CheckpointType.Element && string.IsNullOrWhiteSpace(selector))
        {
            throw new ArgumentException("Parameter 'selector' is required for checkpoint_type 'element'");
        }

        // Get context from tool call metadata (if available)
        var taskId = toolCall.Parameters.TryGetValue("task_id", out var taskIdObj) && taskIdObj is Guid guid 
            ? guid 
            : Guid.Empty;
        var environment = GetOptionalParameter(toolCall, "environment", "dev");
        var browser = GetOptionalParameter(toolCall, "browser", "chromium");
        var viewport = GetOptionalParameter(toolCall, "viewport", "1920x1080");

        // Create visual checkpoint
        var checkpoint = new VisualCheckpoint
        {
            Name = checkpointName,
            Type = checkpointType,
            Tolerance = tolerance,
            Selector = selector,
            Region = region,
            IgnoreSelectors = ignoreSelectors
        };

        if (_options.EnableDetailedLogging)
        {
            _logger.LogInformation(
                "Executing visual check '{CheckpointName}' (type: {CheckpointType}, tolerance: {Tolerance:P2})",
                checkpointName, checkpointType, tolerance);
        }

        // Capture screenshot based on checkpoint type
        byte[] screenshot;
        try
        {
            screenshot = checkpointType switch
            {
                CheckpointType.FullPage => await _browserAgent.TakeFullPageScreenshotBytesAsync(cancellationToken),
                CheckpointType.Element => await _browserAgent.TakeElementScreenshotAsync(selector!, cancellationToken),
                CheckpointType.Region => await _browserAgent.TakeRegionScreenshotAsync(region!, cancellationToken),
                CheckpointType.Viewport => await _browserAgent.TakeViewportScreenshotAsync(cancellationToken),
                _ => throw new NotSupportedException($"Checkpoint type '{checkpointType}' is not supported")
            };

            if (_options.EnableDetailedLogging)
            {
                _logger.LogDebug(
                    "Captured screenshot for visual check '{CheckpointName}' ({Size} bytes)",
                    checkpointName, screenshot.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture screenshot for visual check '{CheckpointName}'", checkpointName);
            throw new InvalidOperationException(
                $"Failed to capture screenshot for visual check '{checkpointName}': {ex.Message}", ex);
        }

        // Compare against baseline
        VisualComparisonResult comparisonResult;
        try
        {
            comparisonResult = await _visualComparisonService.CompareAsync(
                checkpoint,
                screenshot,
                taskId,
                environment,
                browser,
                viewport,
                cancellationToken).ConfigureAwait(false);

            if (_options.EnableDetailedLogging)
            {
                _logger.LogInformation(
                    "Visual check '{CheckpointName}' completed: {Status} (Difference: {Difference:P2}, Tolerance: {Tolerance:P2})",
                    checkpointName,
                    comparisonResult.Passed ? "PASSED" : "FAILED",
                    comparisonResult.DifferencePercentage,
                    tolerance);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Visual comparison failed for checkpoint '{CheckpointName}'", checkpointName);
            throw new InvalidOperationException(
                $"Visual comparison failed for checkpoint '{checkpointName}': {ex.Message}", ex);
        }

        // Return result as dictionary for tool execution result
        var resultData = new Dictionary<string, object>
        {
            ["checkpoint_name"] = checkpointName,
            ["passed"] = comparisonResult.Passed,
            ["difference_percentage"] = comparisonResult.DifferencePercentage,
            ["tolerance"] = tolerance,
            ["pixels_different"] = comparisonResult.PixelsDifferent,
            ["total_pixels"] = comparisonResult.TotalPixels,
            ["comparison_id"] = comparisonResult.Id,
            ["baseline_path"] = comparisonResult.BaselinePath ?? string.Empty,
            ["actual_path"] = comparisonResult.ActualPath ?? string.Empty,
            ["diff_path"] = comparisonResult.DiffPath ?? string.Empty
        };

        if (comparisonResult.SsimScore.HasValue)
        {
            resultData["ssim_score"] = comparisonResult.SsimScore.Value;
        }

        if (!string.IsNullOrWhiteSpace(comparisonResult.DifferenceType))
        {
            resultData["difference_type"] = comparisonResult.DifferenceType;
        }

        return resultData;
    }

    // Mobile Device Emulation Tool Implementations

    private async Task<object?> ExecuteSetDeviceEmulationAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var deviceName = GetOptionalParameter<string?>(toolCall, "device_name", null);

        DeviceProfile device;
        
        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            // Use predefined device from DevicePresets
            device = DevicePresets.GetDevice(deviceName);
            if (device == null)
            {
                var availableDevices = string.Join(", ", DevicePresets.GetAllDevices().Keys);
                throw new ArgumentException(
                    $"Unknown device '{deviceName}'. Available devices: {availableDevices}");
            }
        }
        else
        {
            // Build custom device profile from parameters
            var viewportWidth = GetOptionalParameter<int?>(toolCall, "viewport_width", null);
            var viewportHeight = GetOptionalParameter<int?>(toolCall, "viewport_height", null);
            
            if (!viewportWidth.HasValue || !viewportHeight.HasValue)
            {
                throw new ArgumentException("Either 'device_name' or both 'viewport_width' and 'viewport_height' must be specified");
            }

            var userAgent = GetOptionalParameter<string?>(toolCall, "user_agent", null) 
                ?? "Mozilla/5.0 (Linux; Android 13) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36";
            var deviceScaleFactorStr = GetOptionalParameter<string?>(toolCall, "device_scale_factor", "1.0");
            double deviceScaleFactor;
            if (!double.TryParse(deviceScaleFactorStr, out deviceScaleFactor))
            {
                deviceScaleFactor = 1.0;
            }
            var hasTouch = GetOptionalParameter(toolCall, "has_touch", true);
            var isMobile = GetOptionalParameter(toolCall, "is_mobile", true);

            device = new DeviceProfile
            {
                Name = "Custom Device",
                UserAgent = userAgent,
                Viewport = new ViewportSize(viewportWidth.Value, viewportHeight.Value),
                DeviceScaleFactor = deviceScaleFactor,
                HasTouch = hasTouch,
                IsMobile = isMobile
            };
        }

        await _browserAgent.SetDeviceEmulationAsync(device, cancellationToken).ConfigureAwait(false);
        
        return new Dictionary<string, object>
        {
            ["device_name"] = device.Name,
            ["viewport"] = $"{device.Viewport.Width}x{device.Viewport.Height}",
            ["device_scale_factor"] = device.DeviceScaleFactor,
            ["platform"] = device.Platform ?? "unknown"
        };
    }

    private async Task<object?> ExecuteSetGeolocationAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var preset = GetOptionalParameter<string?>(toolCall, "preset", null);

        double latitude, longitude;
        double? accuracy = null;

        if (!string.IsNullOrWhiteSpace(preset))
        {
            // Use preset location
            var coordinates = preset.ToLowerInvariant() switch
            {
                "sanfrancisco" or "san francisco" or "sf" => GeolocationCoordinates.SanFrancisco,
                "newyork" or "new york" or "nyc" => GeolocationCoordinates.NewYork,
                "london" => GeolocationCoordinates.London,
                "tokyo" => GeolocationCoordinates.Tokyo,
                "sydney" => GeolocationCoordinates.Sydney,
                "paris" => GeolocationCoordinates.Paris,
                _ => throw new ArgumentException(
                    $"Unknown preset location '{preset}'. Available presets: SanFrancisco, NewYork, London, Tokyo, Sydney, Paris")
            };

            latitude = coordinates.Latitude;
            longitude = coordinates.Longitude;
            accuracy = coordinates.Accuracy;
        }
        else
        {
            // Use custom coordinates
            var latitudeStr = GetOptionalParameter<string?>(toolCall, "latitude", null);
            var longitudeStr = GetOptionalParameter<string?>(toolCall, "longitude", null);
            
            if (string.IsNullOrWhiteSpace(latitudeStr) || string.IsNullOrWhiteSpace(longitudeStr))
            {
                throw new ArgumentException("Either 'preset' or both 'latitude' and 'longitude' must be specified");
            }

            if (!double.TryParse(latitudeStr, out latitude))
            {
                throw new ArgumentException($"Invalid latitude value: '{latitudeStr}'");
            }

            if (!double.TryParse(longitudeStr, out longitude))
            {
                throw new ArgumentException($"Invalid longitude value: '{longitudeStr}'");
            }

            var accuracyStr = GetOptionalParameter<string?>(toolCall, "accuracy", null);
            if (!string.IsNullOrWhiteSpace(accuracyStr) && double.TryParse(accuracyStr, out var parsedAccuracy))
            {
                accuracy = parsedAccuracy;
            }
        }

        await _browserAgent.SetGeolocationAsync(latitude, longitude, accuracy, cancellationToken).ConfigureAwait(false);
        
        return new Dictionary<string, object>
        {
            ["latitude"] = latitude,
            ["longitude"] = longitude,
            ["accuracy"] = accuracy ?? 0
        };
    }

    private async Task<object?> ExecuteSetTimezoneAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var timezoneId = GetRequiredParameter<string>(toolCall, "timezone_id");
        await _browserAgent.SetTimezoneAsync(timezoneId, cancellationToken).ConfigureAwait(false);
        
        return new Dictionary<string, object>
        {
            ["timezone_id"] = timezoneId,
            ["warning"] = "Timezone changes after context creation have limited support in Playwright"
        };
    }

    private async Task<object?> ExecuteSetLocaleAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var locale = GetRequiredParameter<string>(toolCall, "locale");
        await _browserAgent.SetLocaleAsync(locale, cancellationToken).ConfigureAwait(false);
        
        return new Dictionary<string, object>
        {
            ["locale"] = locale
        };
    }

    private async Task<object?> ExecuteGrantPermissionsAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var permissionsParam = GetRequiredParameter<object>(toolCall, "permissions");
        
        string[] permissions;
        if (permissionsParam is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            permissions = jsonElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }
        else if (permissionsParam is string[] stringArray)
        {
            permissions = stringArray;
        }
        else
        {
            throw new ArgumentException("Parameter 'permissions' must be an array of strings");
        }

        if (permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission must be specified");
        }

        await _browserAgent.GrantPermissionsAsync(permissions, cancellationToken).ConfigureAwait(false);
        
        return new Dictionary<string, object>
        {
            ["granted_permissions"] = permissions,
            ["count"] = permissions.Length
        };
    }

    private async Task<object?> ExecuteClearPermissionsAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        await _browserAgent.ClearPermissionsAsync(cancellationToken).ConfigureAwait(false);
        
        return new Dictionary<string, object>
        {
            ["message"] = "All permissions cleared successfully"
        };
    }

    #endregion

    #region Network Interception Tool Execution

    private async Task<object?> ExecuteMockResponseAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var interceptor = _browserAgent.GetNetworkInterceptor();
        if (interceptor == null)
        {
            return new Dictionary<string, object>
            {
                ["error"] = "Network interceptor not available"
            };
        }

        var urlPattern = GetRequiredParameter<string>(toolCall, "url_pattern");
        var status = GetOptionalParameter<int>(toolCall, "status", 200);
        var body = GetOptionalParameter<string>(toolCall, "body", null);
        var contentType = GetOptionalParameter<string>(toolCall, "content_type", "application/json");
        var delayMs = GetOptionalParameter<int>(toolCall, "delay_ms", 0);

        // Parse headers if provided
        Dictionary<string, string>? headers = null;
        if (toolCall.Parameters.TryGetValue("headers", out var headersObj) && headersObj is JsonElement headersJson)
        {
            headers = new Dictionary<string, string>();
            if (headersJson.ValueKind == JsonValueKind.Array)
            {
                headers = headersJson
                    .EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(headerStr => !string.IsNullOrEmpty(headerStr) && headerStr.Contains(':'))
                    .Select(headerStr => headerStr.Split(':', 2))
                    .ToDictionary(
                        parts => parts[0].Trim(),
                        parts => parts[1].Trim()
                    );
            }
        }

        var mockResponse = new MockResponse
        {
            Status = status,
            Body = body,
            ContentType = contentType,
            DelayMs = delayMs > 0 ? delayMs : null,
            Headers = headers
        };

        await interceptor.MockResponseAsync(urlPattern, mockResponse, cancellationToken).ConfigureAwait(false);

        return new Dictionary<string, object>
        {
            ["pattern"] = urlPattern,
            ["status"] = status,
            ["delay_ms"] = delayMs,
            ["message"] = $"Mock response configured for pattern: {urlPattern}"
        };
    }

    private async Task<object?> ExecuteBlockRequestAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var interceptor = _browserAgent.GetNetworkInterceptor();
        if (interceptor == null)
        {
            return new Dictionary<string, object>
            {
                ["error"] = "Network interceptor not available"
            };
        }

        var urlPattern = GetRequiredParameter<string>(toolCall, "url_pattern");
        await interceptor.BlockRequestAsync(urlPattern, cancellationToken).ConfigureAwait(false);

        return new Dictionary<string, object>
        {
            ["pattern"] = urlPattern,
            ["message"] = $"Requests matching '{urlPattern}' will be blocked"
        };
    }

    private async Task<object?> ExecuteInterceptRequestAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var interceptor = _browserAgent.GetNetworkInterceptor();
        if (interceptor == null)
        {
            return new Dictionary<string, object>
            {
                ["error"] = "Network interceptor not available"
            };
        }

        var urlPattern = GetRequiredParameter<string>(toolCall, "url_pattern");
        var action = GetOptionalParameter<string>(toolCall, "action", "continue");

        // For now, we'll set up basic interception based on action
        switch (action.ToLowerInvariant())
        {
            case "abort":
            case "block":
                await interceptor.BlockRequestAsync(urlPattern, cancellationToken).ConfigureAwait(false);
                break;

            case "fulfill":
            case "mock":
                // Default mock response
                await interceptor.MockResponseAsync(urlPattern, new MockResponse
                {
                    Status = 200,
                    Body = "{}",
                    ContentType = "application/json"
                }, cancellationToken).ConfigureAwait(false);
                break;

            case "continue":
            default:
                // Set up pass-through interception (logs but doesn't modify)
                await interceptor.InterceptRequestAsync(urlPattern, async request =>
                {
                    // Return null to continue with original request
                    return await Task.FromResult<InterceptedResponse?>(null);
                }, cancellationToken).ConfigureAwait(false);
                break;
        }

        return new Dictionary<string, object>
        {
            ["pattern"] = urlPattern,
            ["action"] = action,
            ["message"] = $"Request interception configured for pattern: {urlPattern} (action: {action})"
        };
    }

    private async Task<object?> ExecuteGetNetworkLogsAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var interceptor = _browserAgent.GetNetworkInterceptor();
        if (interceptor == null)
        {
            return new Dictionary<string, object>
            {
                ["error"] = "Network interceptor not available",
                ["logs"] = Array.Empty<object>()
            };
        }

        var enableLogging = GetOptionalParameter<bool>(toolCall, "enable_logging", true);
        if (enableLogging && !interceptor.IsNetworkLoggingEnabled)
        {
            await interceptor.SetNetworkLoggingAsync(true, cancellationToken).ConfigureAwait(false);
        }

        var logs = await interceptor.GetNetworkLogsAsync(cancellationToken).ConfigureAwait(false);

        return new Dictionary<string, object>
        {
            ["count"] = logs.Count,
            ["logs"] = logs.Select(log => new Dictionary<string, object>
            {
                ["url"] = log.Url,
                ["method"] = log.Method,
                ["status_code"] = log.StatusCode ?? 0,
                ["resource_type"] = log.ResourceType ?? "unknown",
                ["duration_ms"] = log.DurationMs ?? 0,
                ["was_blocked"] = log.WasBlocked,
                ["was_mocked"] = log.WasMocked,
                ["timestamp"] = log.Timestamp.ToString("O")
            }).ToList()
        };
    }

    private async Task<object?> ExecuteClearInterceptionsAsync(ToolCall toolCall, CancellationToken cancellationToken)
    {
        var interceptor = _browserAgent.GetNetworkInterceptor();
        if (interceptor == null)
        {
            return new Dictionary<string, object>
            {
                ["message"] = "Network interceptor not available"
            };
        }

        var clearLogs = GetOptionalParameter<bool>(toolCall, "clear_logs", false);

        await interceptor.ClearInterceptionsAsync(cancellationToken).ConfigureAwait(false);

        if (clearLogs)
        {
            await interceptor.ClearNetworkLogsAsync(cancellationToken).ConfigureAwait(false);
        }

        return new Dictionary<string, object>
        {
            ["message"] = "All network interceptions cleared",
            ["logs_cleared"] = clearLogs
        };
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
        catch (InvalidCastException ex)
        {
            throw new ArgumentException(
                $"Parameter '{parameterName}' in tool call '{toolCall.ToolName}' " +
                $"cannot be converted to type {typeof(T).Name}", ex);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"Parameter '{parameterName}' in tool call '{toolCall.ToolName}' " +
                $"cannot be converted to type {typeof(T).Name}", ex);
        }
        catch (FormatException ex)
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
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize parameter '{ParameterName}' in tool call '{ToolName}' to type {TypeName}. Returning default value.", parameterName, toolCall.ToolName, typeof(T).Name);
            return defaultValue;
        }
        catch (InvalidCastException ex)
        {
            _logger.LogWarning(ex, "Failed to cast parameter '{ParameterName}' in tool call '{ToolName}' to type {TypeName}. Returning default value.", parameterName, toolCall.ToolName, typeof(T).Name);
            return defaultValue;
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to format parameter '{ParameterName}' in tool call '{ToolName}' to type {TypeName}. Returning default value.", parameterName, toolCall.ToolName, typeof(T).Name);
            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error converting parameter '{ParameterName}' in tool call '{ToolName}' to type {TypeName}. Returning default value.", parameterName, toolCall.ToolName, typeof(T).Name);
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

    #region Selector Healing Helpers

    private async Task<EvoAITest.Core.Models.SelfHealing.HealedSelector?> TryHealSelectorAsync(
        string failedSelector, 
        string? expectedText,
        CancellationToken cancellationToken)
    {
        if (_selectorHealingService == null)
            return null;

        try
        {
            var pageState = await _browserAgent.GetPageStateAsync(cancellationToken).ConfigureAwait(false);
            var screenshotBase64 = await _browserAgent.TakeScreenshotAsync(cancellationToken).ConfigureAwait(false);
            byte[]? screenshot = !string.IsNullOrEmpty(screenshotBase64) 
                ? Convert.FromBase64String(screenshotBase64) 
                : null;
            
            return await _selectorHealingService.HealSelectorAsync(
                failedSelector,
                pageState,
                expectedText,
                screenshot,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Selector healing attempt failed for selector: {Selector}", failedSelector);
            return null;
        }
    }

    private async Task LogSuccessfulHealingAsync(
        string originalSelector,
        EvoAITest.Core.Models.SelfHealing.HealedSelector healedSelector,
        CancellationToken cancellationToken)
    {
        if (_selectorHealingService == null)
            return;

        try
        {
            await _selectorHealingService.SaveHealingHistoryAsync(
                healedSelector,
                null, // TaskId is null when healing occurs outside of a specific task context
                true,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log healing history");
        }
    }

    private static bool IsSelectorError(Exception ex)
    {
        var message = ex.Message;
        if (string.IsNullOrWhiteSpace(message))
            return false;

        message = message.ToLowerInvariant();

        foreach (var pattern in SelectorErrorPatterns)
        {
            if (message.Contains(pattern))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to extract expected text from a CSS selector containing text-based selectors.
    /// </summary>
    private static string? ExtractExpectedTextFromSelector(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return null;
            
        // Extract from :contains() pseudo-class
        var containsMatch = ContainsRegex.Match(selector);
        if (containsMatch.Success)
            return containsMatch.Groups[1].Value;
            
        // Extract from :has-text() pseudo-class
        var hasTextMatch = HasTextRegex.Match(selector);
        if (hasTextMatch.Success)
            return hasTextMatch.Groups[1].Value;
            
        // Extract from text attribute
        var textAttrMatch = TextAttrRegex.Match(selector);
        if (textAttrMatch.Success)
            return textAttrMatch.Groups[1].Value;
        
        return null;
    }

    #endregion

    #endregion
}
