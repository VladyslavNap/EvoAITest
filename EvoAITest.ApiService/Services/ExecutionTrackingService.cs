using EvoAITest.ApiService.Hubs;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.Analytics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EvoAITest.ApiService.Services;

/// <summary>
/// Service for tracking execution metrics and broadcasting real-time updates.
/// Coordinates between IAnalyticsService for data persistence and SignalR for real-time updates.
/// </summary>
public sealed class ExecutionTrackingService
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IHubContext<AnalyticsHub> _analyticsHub;
    private readonly ILogger<ExecutionTrackingService> _logger;

    public ExecutionTrackingService(
        IAnalyticsService analyticsService,
        IHubContext<AnalyticsHub> analyticsHub,
        ILogger<ExecutionTrackingService> logger)
    {
        _analyticsService = analyticsService;
        _analyticsHub = analyticsHub;
        _logger = logger;
    }

    /// <summary>
    /// Tracks the start of a task execution.
    /// </summary>
    public async Task TrackExecutionStartAsync(
        Guid taskId,
        string taskName,
        int totalSteps,
        string? targetUrl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSteps);

        try
        {
            var metrics = new ExecutionMetrics
            {
                TaskId = taskId,
                TaskName = taskName,
                Status = ExecutionStatus.Success, // Will be updated
                CurrentStep = 0,
                TotalSteps = totalSteps,
                DurationMs = 0,
                IsActive = true,
                StepsCompleted = 0,
                StepsFailed = 0,
                CompletionPercentage = 0,
                TargetUrl = targetUrl,
                RecordedAt = DateTimeOffset.UtcNow
            };

            await _analyticsService.RecordMetricsAsync(metrics, cancellationToken);
            await _analyticsHub.SendExecutionStarted(metrics);

            _logger.LogInformation(
                "Execution started for Task {TaskId} ({TaskName}) with {TotalSteps} steps",
                taskId,
                taskName,
                totalSteps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track execution start for Task {TaskId}", taskId);
            // Don't throw - tracking shouldn't break execution
        }
    }

    /// <summary>
    /// Tracks progress during task execution.
    /// </summary>
    public async Task TrackExecutionProgressAsync(
        Guid taskId,
        int currentStep,
        string currentAction,
        long durationMs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentAction);
        ArgumentOutOfRangeException.ThrowIfNegative(currentStep);
        ArgumentOutOfRangeException.ThrowIfNegative(durationMs);

        try
        {
            await _analyticsService.UpdateExecutionProgressAsync(
                taskId,
                currentStep,
                currentAction,
                durationMs,
                cancellationToken);

            // Get the updated metrics to broadcast
            var metrics = await _analyticsService.GetTaskMetricsAsync(taskId, includeInactive: false, cancellationToken);
            var latestMetric = metrics.FirstOrDefault();

            if (latestMetric != null)
            {
                await _analyticsHub.SendExecutionProgress(latestMetric);
            }

            _logger.LogDebug(
                "Execution progress for Task {TaskId}: Step {CurrentStep}, Action: {Action}",
                taskId,
                currentStep,
                currentAction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track execution progress for Task {TaskId}", taskId);
            // Don't throw - tracking shouldn't break execution
        }
    }

    /// <summary>
    /// Tracks the completion of a task execution.
    /// </summary>
    public async Task TrackExecutionCompleteAsync(
        Guid taskId,
        ExecutionStatus status,
        long durationMs,
        string? errorMessage = null,
        bool healingAttempted = false,
        bool healingSuccessful = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(durationMs);

        try
        {
            await _analyticsService.CompleteExecutionAsync(
                taskId,
                status,
                durationMs,
                errorMessage,
                cancellationToken);

            // Get the completed metrics to broadcast
            var metrics = await _analyticsService.GetTaskMetricsAsync(taskId, includeInactive: true, cancellationToken);
            var completedMetric = metrics.FirstOrDefault(m => !m.IsActive);

            if (completedMetric != null)
            {
                // Update healing information
                completedMetric.HealingAttempted = healingAttempted;
                completedMetric.HealingSuccessful = healingSuccessful;
                
                await _analyticsHub.SendExecutionCompleted(completedMetric);
            }

            // Broadcast updated dashboard analytics
            await BroadcastDashboardUpdateAsync(cancellationToken);

            _logger.LogInformation(
                "Execution completed for Task {TaskId} with status {Status} in {DurationMs}ms",
                taskId,
                status,
                durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track execution completion for Task {TaskId}", taskId);
            // Don't throw - tracking shouldn't break execution
        }
    }

    /// <summary>
    /// Tracks a step completion.
    /// </summary>
    public async Task TrackStepCompleteAsync(
        Guid taskId,
        int stepNumber,
        bool success,
        long durationMs,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stepNumber);
        ArgumentOutOfRangeException.ThrowIfNegative(durationMs);

        try
        {
            // Update step counts in the existing active metric record
            await _analyticsService.UpdateStepCountsAsync(taskId, success, cancellationToken);

            _logger.LogDebug(
                "Step {StepNumber} completed for Task {TaskId}: Success={Success}, Duration={DurationMs}ms",
                stepNumber,
                taskId,
                success,
                durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track step completion for Task {TaskId}", taskId);
            // Don't throw - tracking shouldn't break execution
        }
    }

    /// <summary>
    /// Broadcasts a dashboard analytics update to all connected clients.
    /// </summary>
    public async Task BroadcastDashboardUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _analyticsService.GetDashboardAnalyticsAsync(cancellationToken);
            await _analyticsHub.SendDashboardAnalytics(analytics);

            _logger.LogDebug("Dashboard analytics broadcasted to all clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast dashboard update");
            // Don't throw - broadcasting shouldn't break execution
        }
    }

    /// <summary>
    /// Broadcasts active executions update.
    /// </summary>
    public async Task BroadcastActiveExecutionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var activeExecutions = await _analyticsService.GetActiveExecutionsAsync(cancellationToken);
            await _analyticsHub.SendActiveExecutionsUpdate(activeExecutions);

            _logger.LogDebug("Active executions update broadcasted: {Count} active", activeExecutions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast active executions");
            // Don't throw - broadcasting shouldn't break execution
        }
    }

    /// <summary>
    /// Broadcasts system health update.
    /// </summary>
    public async Task BroadcastSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await _analyticsService.GetSystemHealthAsync(cancellationToken);
            await _analyticsHub.SendSystemHealthUpdate(health);

            _logger.LogDebug("System health update broadcasted: Status={Status}", health.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast system health");
            // Don't throw - broadcasting shouldn't break execution
        }
    }
}
