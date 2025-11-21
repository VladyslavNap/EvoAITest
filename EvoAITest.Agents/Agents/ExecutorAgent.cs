using System.Diagnostics;
using EvoAITest.Agents.Abstractions;
using EvoAITest.Agents.Models;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using Microsoft.Extensions.Logging;
using AgentsTaskStatus = EvoAITest.Agents.Models.TaskStatus;

namespace EvoAITest.Agents.Agents;

/// <summary>
/// Default implementation of <see cref="IExecutor"/> that executes agent plans by coordinating
/// tool execution through the <see cref="IToolExecutor"/> service.
/// </summary>
/// <remarks>
/// <para>
/// The ExecutorAgent is responsible for:
/// - Executing complete execution plans step by step
/// - Converting <see cref="AgentStep"/> actions to <see cref="ToolCall"/> objects
/// - Managing execution state and progress tracking
/// - Collecting execution statistics and metrics
/// - Handling errors with appropriate retry and recovery strategies
/// - Supporting pause, resume, and cancellation operations
/// </para>
/// <para>
/// The executor integrates with the Tool Executor service which handles low-level browser
/// operations, retry logic, and error recovery. This separation allows the ExecutorAgent
/// to focus on high-level orchestration while the ToolExecutor handles resilience patterns.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var executor = serviceProvider.GetRequiredService&lt;IExecutor&gt;();
/// var plan = await planner.CreatePlanAsync(task, context);
/// 
/// var result = await executor.ExecutePlanAsync(plan, context, cancellationToken);
/// 
/// if (result.Success)
/// {
///     Console.WriteLine($"Plan executed successfully in {result.DurationMs}ms");
///     Console.WriteLine($"Completed {result.Statistics.SuccessfulSteps}/{result.Statistics.TotalSteps} steps");
/// }
/// else
/// {
///     Console.WriteLine($"Plan execution failed: {result.ErrorMessage}");
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class ExecutorAgent : IExecutor
{
    private readonly IToolExecutor _toolExecutor;
    private readonly IBrowserAgent _browserAgent;
    private readonly ILogger<ExecutorAgent> _logger;
    
    // Track paused/cancelled tasks
    private readonly Dictionary<string, CancellationTokenSource> _taskCancellationSources = new();
    private readonly Dictionary<string, TaskExecutionState> _taskStates = new();
    private readonly object _stateLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutorAgent"/> class.
    /// </summary>
    /// <param name="toolExecutor">The tool executor for executing individual browser operations.</param>
    /// <param name="browserAgent">The browser agent for capturing page state and screenshots.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public ExecutorAgent(
        IToolExecutor toolExecutor,
        IBrowserAgent browserAgent,
        ILogger<ExecutorAgent> logger)
    {
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _browserAgent = browserAgent ?? throw new ArgumentNullException(nameof(browserAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AgentStepResult> ExecuteStepAsync(
        AgentStep step,
        Abstractions.ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(context);

        var stepStopwatch = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Executing step {StepNumber}: {Action} [SessionId: {SessionId}]",
            step.StepNumber,
            step.Action?.Type.ToString() ?? "Unknown",
            context.SessionId);

        var stepResult = new AgentStepResult
        {
            StepId = step.Id,
            StartedAt = startedAt
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate step has an action
            if (step.Action == null)
            {
                throw new InvalidOperationException($"Step {step.StepNumber} has no action defined");
            }

            // Convert AgentStep to ToolCall
            var toolCall = ConvertStepToToolCall(step, context.SessionId);

            // Create timeout for this step
            using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            stepCts.CancelAfter(TimeSpan.FromMilliseconds(step.TimeoutMs));

            // Execute the tool with configured retry behavior
            var toolResult = await _toolExecutor.ExecuteToolAsync(toolCall, stepCts.Token)
                .ConfigureAwait(false);

            stepStopwatch.Stop();

            // Build step result from tool execution result
            stepResult.Success = toolResult.Success;
            stepResult.ExecutionResult = ConvertToolResultToExecutionResult(toolResult, step);
            stepResult.RetryAttempts = toolResult.AttemptCount - 1; // Subtract initial attempt
            stepResult.DurationMs = (long)toolResult.ExecutionDuration.TotalMilliseconds;
            stepResult.CompletedAt = DateTimeOffset.UtcNow;

            if (!toolResult.Success)
            {
                stepResult.Error = toolResult.Error;

                // Capture screenshot on failure
                try
                {
                    if (stepResult.ExecutionResult != null)
                    {
                        stepResult.ExecutionResult.Screenshot = await _browserAgent
                            .TakeScreenshotAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception screenshotEx)
                {
                    _logger.LogWarning(screenshotEx, 
                        "Failed to capture screenshot after step failure for step {StepNumber}",
                        step.StepNumber);
                }
            }

            // Extract data if the tool returned a result
            if (toolResult.Success && toolResult.Result != null)
            {
                stepResult.ExtractedData["result"] = toolResult.Result;
            }

            // Perform validation if rules are defined
            if (step.ValidationRules.Count > 0)
            {
                stepResult.ValidationResults = await ValidateStepAsync(step, stepResult, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Log step completion
            if (stepResult.Success)
            {
                _logger.LogInformation(
                    "Step {StepNumber} completed successfully in {Duration}ms [Attempts: {Attempts}]",
                    step.StepNumber,
                    stepResult.DurationMs,
                    toolResult.AttemptCount);
            }
            else
            {
                _logger.LogError(
                    "Step {StepNumber} failed after {Duration}ms [Attempts: {Attempts}]: {Error}",
                    step.StepNumber,
                    stepResult.DurationMs,
                    toolResult.AttemptCount,
                    stepResult.Error?.Message ?? "Unknown error");
            }

            return stepResult;
        }
        catch (OperationCanceledException ex)
        {
            stepStopwatch.Stop();
            stepResult.Success = false;
            stepResult.Error = ex;
            stepResult.DurationMs = stepStopwatch.ElapsedMilliseconds;
            stepResult.CompletedAt = DateTimeOffset.UtcNow;

            _logger.LogWarning(
                "Step {StepNumber} was cancelled after {Duration}ms",
                step.StepNumber,
                stepResult.DurationMs);

            return stepResult;
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            stepResult.Success = false;
            stepResult.Error = ex;
            stepResult.DurationMs = stepStopwatch.ElapsedMilliseconds;
            stepResult.CompletedAt = DateTimeOffset.UtcNow;

            _logger.LogError(ex,
                "Unexpected error executing step {StepNumber} after {Duration}ms",
                step.StepNumber,
                stepResult.DurationMs);

            return stepResult;
        }
    }

    /// <inheritdoc />
    public async Task<AgentTaskResult> ExecutePlanAsync(
        ExecutionPlan plan,
        Abstractions.ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var planStopwatch = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.UtcNow;
        var taskId = plan.TaskId;

        _logger.LogInformation(
            "Starting plan execution for task {TaskId}: {StepCount} steps [PlanId: {PlanId}, SessionId: {SessionId}]",
            taskId,
            plan.Steps.Count,
            plan.Id,
            context.SessionId);

        // Register task cancellation source for pause/cancel support
        var taskCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (_stateLock)
        {
            if (_taskCancellationSources.ContainsKey(taskId))
            {
                taskCts.Dispose();
                throw new InvalidOperationException($"Task {taskId} is already executing");
            }
            _taskCancellationSources[taskId] = taskCts;
            _taskStates[taskId] = new TaskExecutionState
            {
                TaskId = taskId,
                Status = AgentsTaskStatus.Executing,
                StartedAt = startedAt
            };
        }

        var taskResult = new AgentTaskResult
        {
            TaskId = taskId,
            StartedAt = startedAt,
            Status = AgentsTaskStatus.Executing,
            Metadata = new Dictionary<string, object>
            {
                ["plan_id"] = plan.Id,
                ["session_id"] = context.SessionId,
                ["estimated_duration_ms"] = plan.EstimatedDurationMs,
                ["plan_confidence"] = plan.Confidence
            }
        };

        try
        {
            // Execute each step in sequence
            foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
            {
                taskCts.Token.ThrowIfCancellationRequested();

                // Check if task is paused
                lock (_stateLock)
                {
                    if (_taskStates.TryGetValue(taskId, out var state) && state.Status == AgentsTaskStatus.Paused)
                    {
                        _logger.LogInformation(
                            "Task {TaskId} is paused, waiting for resume signal",
                            taskId);

                        // Wait for resume or cancellation
                        while (state.Status == AgentsTaskStatus.Paused && !taskCts.Token.IsCancellationRequested)
                        {
                            Monitor.Wait(_stateLock, 100);
                        }

                        if (taskCts.Token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException("Task was cancelled while paused");
                        }
                    }
                }

                _logger.LogDebug(
                    "Executing step {StepNumber}/{TotalSteps}: {Action}",
                    step.StepNumber,
                    plan.Steps.Count,
                    step.Action?.Type.ToString() ?? "Unknown");

                // Execute the step
                var stepResult = await ExecuteStepAsync(step, context, taskCts.Token)
                    .ConfigureAwait(false);

                taskResult.StepResults.Add(stepResult);

                // Update context with step result
                context.PreviousSteps.Add(stepResult);

                // Check if step failed and is not optional
                if (!stepResult.Success && !step.IsOptional)
                {
                    _logger.LogWarning(
                        "Step {StepNumber} failed and is not optional, stopping execution",
                        step.StepNumber);

                    taskResult.Success = false;
                    taskResult.Status = AgentsTaskStatus.Failed;
                    taskResult.ErrorMessage = $"Step {step.StepNumber} failed: {stepResult.Error?.Message ?? "Unknown error"}";
                    taskResult.Error = stepResult.Error;

                    break;
                }

                // If step is optional and failed, log and continue
                if (!stepResult.Success && step.IsOptional)
                {
                    _logger.LogInformation(
                        "Step {StepNumber} failed but is optional, continuing execution",
                        step.StepNumber);
                }
            }

            planStopwatch.Stop();
            taskResult.CompletedAt = DateTimeOffset.UtcNow;
            taskResult.DurationMs = planStopwatch.ElapsedMilliseconds;

            // Determine final status if not already set
            if (taskResult.Status == AgentsTaskStatus.Executing)
            {
                var allStepsExecuted = taskResult.StepResults.Count == plan.Steps.Count;
                var allSuccessful = taskResult.StepResults.All(r => r.Success);
                var someSuccessful = taskResult.StepResults.Any(r => r.Success);

                if (allStepsExecuted && allSuccessful)
                {
                    taskResult.Success = true;
                    taskResult.Status = AgentsTaskStatus.Completed;
                }
                else if (someSuccessful)
                {
                    // Partial success - some steps succeeded but not all
                    taskResult.Success = false;
                    taskResult.Status = AgentsTaskStatus.Failed;
                    taskResult.ErrorMessage = "Some steps failed during execution (partial success)";
                }
                else
                {
                    taskResult.Success = false;
                    taskResult.Status = AgentsTaskStatus.Failed;
                    taskResult.ErrorMessage = "All steps failed during execution";
                }
            }

            // Collect execution statistics
            taskResult.Statistics = CalculateStatistics(taskResult.StepResults, planStopwatch.Elapsed);

            // Capture final screenshot
            try
            {
                var finalScreenshot = await _browserAgent.TakeScreenshotAsync(cancellationToken)
                    .ConfigureAwait(false);
                taskResult.Screenshots.Add(finalScreenshot);
            }
            catch (Exception screenshotEx)
            {
                _logger.LogWarning(screenshotEx, "Failed to capture final screenshot for task {TaskId}", taskId);
            }

            // Log completion
            _logger.LogInformation(
                "Plan execution completed for task {TaskId}: Status={Status}, Steps={Completed}/{Total}, Duration={Duration}ms, Success={Success}",
                taskId,
                taskResult.Status,
                taskResult.StepResults.Count,
                plan.Steps.Count,
                taskResult.DurationMs,
                taskResult.Success);

            return taskResult;
        }
        catch (OperationCanceledException ex)
        {
            planStopwatch.Stop();
            taskResult.Success = false;
            taskResult.Status = AgentsTaskStatus.Cancelled;
            taskResult.Error = ex;
            taskResult.ErrorMessage = "Task execution was cancelled";
            taskResult.CompletedAt = DateTimeOffset.UtcNow;
            taskResult.DurationMs = planStopwatch.ElapsedMilliseconds;
            taskResult.Statistics = CalculateStatistics(taskResult.StepResults, planStopwatch.Elapsed);

            _logger.LogWarning(
                "Plan execution cancelled for task {TaskId} after {Duration}ms [{StepsCompleted}/{TotalSteps} steps completed]",
                taskId,
                taskResult.DurationMs,
                taskResult.StepResults.Count,
                plan.Steps.Count);

            return taskResult;
        }
        catch (Exception ex)
        {
            planStopwatch.Stop();
            taskResult.Success = false;
            taskResult.Status = AgentsTaskStatus.Failed;
            taskResult.Error = ex;
            taskResult.ErrorMessage = $"Unexpected error during plan execution: {ex.Message}";
            taskResult.CompletedAt = DateTimeOffset.UtcNow;
            taskResult.DurationMs = planStopwatch.ElapsedMilliseconds;
            taskResult.Statistics = CalculateStatistics(taskResult.StepResults, planStopwatch.Elapsed);

            _logger.LogError(ex,
                "Unexpected error during plan execution for task {TaskId} after {Duration}ms",
                taskId,
                taskResult.DurationMs);

            return taskResult;
        }
        finally
        {
            // Cleanup task state
            lock (_stateLock)
            {
                if (_taskCancellationSources.TryGetValue(taskId, out var cts))
                {
                    _taskCancellationSources.Remove(taskId);
                    cts.Dispose();
                }
                _taskStates.Remove(taskId);
            }
        }
    }

    /// <inheritdoc />
    public Task PauseExecutionAsync(string taskId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        lock (_stateLock)
        {
            if (!_taskStates.TryGetValue(taskId, out var state))
            {
                throw new InvalidOperationException($"No active execution found for task {taskId}");
            }

            if (state.Status != AgentsTaskStatus.Executing)
            {
                throw new InvalidOperationException($"Cannot pause task {taskId} with status {state.Status}");
            }

            state.Status = AgentsTaskStatus.Paused;
            state.PausedAt = DateTimeOffset.UtcNow;

            _logger.LogInformation("Task {TaskId} execution paused", taskId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResumeExecutionAsync(string taskId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        lock (_stateLock)
        {
            if (!_taskStates.TryGetValue(taskId, out var state))
            {
                throw new InvalidOperationException($"No paused execution found for task {taskId}");
            }

            if (state.Status != AgentsTaskStatus.Paused)
            {
                throw new InvalidOperationException($"Cannot resume task {taskId} with status {state.Status}");
            }

            state.Status = AgentsTaskStatus.Executing;
            state.ResumedAt = DateTimeOffset.UtcNow;

            // Signal any waiting threads
            Monitor.PulseAll(_stateLock);

            _logger.LogInformation("Task {TaskId} execution resumed", taskId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelExecutionAsync(string taskId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        lock (_stateLock)
        {
            if (!_taskCancellationSources.TryGetValue(taskId, out var cts))
            {
                throw new InvalidOperationException($"No active execution found for task {taskId}");
            }

            if (_taskStates.TryGetValue(taskId, out var state))
            {
                state.Status = AgentsTaskStatus.Cancelled;
                state.CancelledAt = DateTimeOffset.UtcNow;
            }

            // Cancel the task
            cts.Cancel();

            // Signal any waiting threads
            Monitor.PulseAll(_stateLock);

            _logger.LogInformation("Task {TaskId} execution cancelled", taskId);
        }

        return Task.CompletedTask;
    }

    // ============================================================
    // Private Helper Methods
    // ============================================================

    /// <summary>
    /// Converts an <see cref="AgentStep"/> to a <see cref="ToolCall"/> for execution.
    /// </summary>
    private ToolCall ConvertStepToToolCall(AgentStep step, string sessionId)
    {
        if (step.Action == null)
        {
            throw new InvalidOperationException($"Step {step.StepNumber} has no action");
        }

        var toolName = MapActionTypeToToolName(step.Action.Type);
        var parameters = ExtractToolParameters(step.Action);

        return new ToolCall(
            ToolName: toolName,
            Parameters: parameters,
            Reasoning: step.Reasoning ?? $"Execute step {step.StepNumber}",
            CorrelationId: sessionId
        );
    }

    /// <summary>
    /// Maps <see cref="ActionType"/> to browser tool name.
    /// </summary>
    private string MapActionTypeToToolName(ActionType actionType)
    {
        return actionType switch
        {
            ActionType.Navigate => "navigate",
            ActionType.Click => "click",
            ActionType.Type => "type",
            ActionType.Fill => "type",
            ActionType.Select => "select_option",
            ActionType.WaitForElement => "wait_for_element",
            ActionType.Screenshot => "take_screenshot",
            ActionType.ExtractText => "get_text",
            ActionType.Verify => "verify_element_exists",
            _ => throw new NotSupportedException($"Action type {actionType} is not supported")
        };
    }

    /// <summary>
    /// Extracts tool parameters from a <see cref="BrowserAction"/>.
    /// </summary>
    private Dictionary<string, object> ExtractToolParameters(BrowserAction action)
    {
        var parameters = new Dictionary<string, object>();

        // Add selector if target is specified
        if (action.Target != null)
        {
            parameters["selector"] = action.Target.Selector ?? string.Empty;
        }

        // Add value/text if specified
        if (!string.IsNullOrEmpty(action.Value))
        {
            if (action.Type == ActionType.Type || action.Type == ActionType.Fill)
            {
                parameters["text"] = action.Value;
            }
            else if (action.Type == ActionType.Navigate)
            {
                parameters["url"] = action.Value;
            }
            else
            {
                parameters["value"] = action.Value;
            }
        }

        // Add timeout if specified
        if (action.TimeoutMs > 0)
        {
            parameters["timeout_ms"] = action.TimeoutMs;
        }

        return parameters;
    }

    /// <summary>
    /// Converts a <see cref="ToolExecutionResult"/> to an <see cref="ExecutionResult"/>.
    /// </summary>
    private ExecutionResult ConvertToolResultToExecutionResult(ToolExecutionResult toolResult, AgentStep step)
    {
        var executionResult = new ExecutionResult
        {
            Success = toolResult.Success,
            ActionId = step.Id,
            Data = toolResult.Result,
            ErrorMessage = toolResult.Error?.Message,
            ErrorDetails = toolResult.Error?.ToString(),
            DurationMs = (long)toolResult.ExecutionDuration.TotalMilliseconds,
            CompletedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>(toolResult.Metadata)
        };

        // Add retry info if retries occurred
        if (toolResult.WasRetried)
        {
            executionResult.RetryInfo = new RetryInfo
            {
                Attempts = toolResult.AttemptCount,
                MaxRetries = toolResult.AttemptCount - 1,
                TotalRetryTimeMs = (long)toolResult.ExecutionDuration.TotalMilliseconds
            };

            if (toolResult.Metadata.TryGetValue("retry_reasons", out var retryReasons) && retryReasons is string[] reasons)
            {
                executionResult.RetryInfo.LastRetryReason = reasons.LastOrDefault();
            }
        }

        return executionResult;
    }

    /// <summary>
    /// Validates a step's execution against its validation rules.
    /// </summary>
    private async Task<List<ValidationResult>> ValidateStepAsync(
        AgentStep step,
        AgentStepResult stepResult,
        CancellationToken cancellationToken)
    {
        var validationResults = new List<ValidationResult>();

        foreach (var rule in step.ValidationRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validationResult = new ValidationResult
            {
                RuleName = rule.Name,
                ExpectedValue = rule.ExpectedValue
            };

            try
            {
                switch (rule.Type)
                {
                    case ValidationType.ElementExists:
                        validationResult.Passed = await ValidateElementExistsAsync(
                            rule.ExpectedValue?.ToString() ?? string.Empty,
                            cancellationToken);
                        break;

                    case ValidationType.ElementText:
                        var actualText = await _browserAgent.GetTextAsync(
                            rule.ExpectedValue?.ToString() ?? string.Empty,
                            cancellationToken);
                        validationResult.ActualValue = actualText;
                        validationResult.Passed = !string.IsNullOrEmpty(actualText);
                        break;

                    case ValidationType.PageTitle:
                        var pageState = await _browserAgent.GetPageStateAsync(cancellationToken);
                        validationResult.ActualValue = pageState.Title;
                        validationResult.Passed = pageState.Title?.Contains(
                            rule.ExpectedValue?.ToString() ?? string.Empty,
                            StringComparison.OrdinalIgnoreCase) ?? false;
                        break;

                    case ValidationType.DataExtracted:
                        validationResult.Passed = stepResult.ExtractedData.ContainsKey(
                            rule.ExpectedValue?.ToString() ?? string.Empty);
                        break;

                    default:
                        validationResult.Passed = false;
                        validationResult.ErrorMessage = $"Validation type {rule.Type} is not supported";
                        break;
                }

                if (!validationResult.Passed && string.IsNullOrEmpty(validationResult.ErrorMessage))
                {
                    validationResult.ErrorMessage = $"Validation failed for rule '{rule.Name}'";
                }
            }
            catch (Exception ex)
            {
                validationResult.Passed = false;
                validationResult.ErrorMessage = $"Validation error: {ex.Message}";

                _logger.LogWarning(ex,
                    "Validation rule '{RuleName}' failed with exception for step {StepNumber}",
                    rule.Name,
                    step.StepNumber);
            }

            validationResults.Add(validationResult);
        }

        return validationResults;
    }

    /// <summary>
    /// Validates that an element exists on the page.
    /// </summary>
    private async Task<bool> ValidateElementExistsAsync(string selector, CancellationToken cancellationToken)
    {
        try
        {
            await _browserAgent.WaitForElementAsync(selector, 5000, cancellationToken);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Calculates execution statistics from step results.
    /// </summary>
    private ExecutionStatistics CalculateStatistics(
        List<AgentStepResult> stepResults,
        TimeSpan totalDuration)
    {
        var totalSteps = stepResults.Count;
        var successfulSteps = stepResults.Count(r => r.Success);
        var failedSteps = stepResults.Count(r => !r.Success);
        var retriedSteps = stepResults.Count(r => r.RetryAttempts > 0);
        var healedSteps = stepResults.Count(r => r.HealingApplied);
        var totalRetries = stepResults.Sum(r => r.RetryAttempts);
        var totalWaitTime = stepResults
            .Where(r => r.ExecutionResult?.Metadata.ContainsKey("wait_time_ms") == true)
            .Sum(r => Convert.ToInt64(r.ExecutionResult!.Metadata["wait_time_ms"]));

        var avgDuration = totalSteps > 0
            ? stepResults.Average(r => r.DurationMs)
            : 0;

        return new ExecutionStatistics
        {
            TotalSteps = totalSteps,
            SuccessfulSteps = successfulSteps,
            FailedSteps = failedSteps,
            RetriedSteps = retriedSteps,
            HealedSteps = healedSteps,
            TotalRetries = totalRetries,
            AverageStepDurationMs = avgDuration,
            TotalWaitTimeMs = totalWaitTime
        };
    }

    /// <summary>
    /// Represents the execution state of a task.
    /// </summary>
    private sealed class TaskExecutionState
    {
        public string TaskId { get; set; } = string.Empty;
        public AgentsTaskStatus Status { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? PausedAt { get; set; }
        public DateTimeOffset? ResumedAt { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
    }
}
