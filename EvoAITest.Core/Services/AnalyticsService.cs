using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Services;

/// <summary>
/// Service for collecting, aggregating, and analyzing execution metrics.
/// Provides real-time analytics for dashboard visualization and monitoring.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly EvoAIDbContext _dbContext;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        EvoAIDbContext dbContext,
        ILogger<AnalyticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RecordMetricsAsync(ExecutionMetrics metrics, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbContext.ExecutionMetrics.Add(metrics);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Recorded execution metrics for Task {TaskId}, Step {CurrentStep}/{TotalSteps}",
                metrics.TaskId,
                metrics.CurrentStep,
                metrics.TotalSteps);
        }
        catch (Exception ex)
        {
            // Metrics recording is non-critical; log the failure but do not disrupt the main execution flow.
            _logger.LogError(ex, "Failed to record execution metrics for Task {TaskId}", metrics.TaskId);
        }
    }

    public async Task<DashboardAnalytics> GetDashboardAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var last24Hours = now.AddHours(-24);
            var lastHour = now.AddHours(-1);
            var today = now.Date;

            // Get active executions
            var activeExecutions = await GetActiveExecutionsAsync(cancellationToken);

            // Get execution counts and rates
            var executions24h = await _dbContext.ExecutionHistory
                .Where(h => h.StartedAt >= last24Hours)
                .ToListAsync(cancellationToken);

            var executionsLastHour = executions24h
                .Where(h => h.StartedAt >= lastHour)
                .ToList();

            var executionsToday = await _dbContext.ExecutionHistory
                .Where(h => h.StartedAt >= today)
                .ToListAsync(cancellationToken);

            // Calculate success rates
            var successRate24h = CalculateSuccessRate(executions24h);
            var successRateLastHour = CalculateSuccessRate(executionsLastHour);
            var successRateToday = CalculateSuccessRate(executionsToday);

            // Calculate average durations
            var avgDuration24h = executions24h.Any()
                ? (long)executions24h.Average(e => e.DurationMs)
                : 0;

            var avgDurationLastHour = executionsLastHour.Any()
                ? (long)executionsLastHour.Average(e => e.DurationMs)
                : 0;

            // Get time series data
            var timeSeries24h = await GetTimeSeriesAsync(
                MetricType.SuccessRate,
                TimeInterval.Hour,
                last24Hours,
                now,
                cancellationToken: cancellationToken);

            var timeSeries7d = await GetTimeSeriesAsync(
                MetricType.SuccessRate,
                TimeInterval.Day,
                now.AddDays(-7),
                now,
                cancellationToken: cancellationToken);

            // Get top tasks
            var topExecutedTasks = await GetTopExecutedTasksAsync(10, cancellationToken);
            var topFailingTasks = await GetTopFailingTasksAsync(10, cancellationToken);
            var slowestTasks = await GetSlowestTasksAsync(10, cancellationToken);

            // Calculate trends
            var trends = await CalculateTrendsAsync(cancellationToken);

            // Get system health
            var systemHealth = await GetSystemHealthAsync(cancellationToken);

            // Get task counts
            var totalTasks = await _dbContext.AutomationTasks.CountAsync(cancellationToken);
            var tasksWithHealing = await _dbContext.ExecutionMetrics
                .Where(m => m.HealingAttempted)
                .Select(m => m.TaskId)
                .Distinct()
                .CountAsync(cancellationToken);

            // Calculate healing success rate
            var healingAttempts = await _dbContext.ExecutionMetrics
                .Where(m => m.HealingAttempted)
                .ToListAsync(cancellationToken);

            var healingSuccessRate = healingAttempts.Any()
                ? (double)healingAttempts.Count(m => m.HealingSuccessful) / healingAttempts.Count * 100
                : 0;

            return new DashboardAnalytics
            {
                CalculatedAt = now,
                ActiveExecutions = activeExecutions,
                ActiveExecutionCount = activeExecutions.Count,
                ExecutionsLast24Hours = executions24h.Count,
                ExecutionsLastHour = executionsLastHour.Count,
                SuccessRateLast24Hours = successRate24h,
                SuccessRateLastHour = successRateLastHour,
                AverageDurationMsLast24Hours = avgDuration24h,
                AverageDurationMsLastHour = avgDurationLastHour,
                TotalExecutionsToday = executionsToday.Count,
                SuccessfulExecutionsToday = executionsToday.Count(e => e.ExecutionStatus == ExecutionStatus.Success),
                FailedExecutionsToday = executionsToday.Count(e => e.ExecutionStatus == ExecutionStatus.Failed),
                SuccessRateToday = successRateToday,
                TotalTasks = totalTasks,
                TasksWithHealingEnabled = tasksWithHealing,
                HealingSuccessRate = healingSuccessRate,
                TimeSeriesLast24Hours = timeSeries24h,
                TimeSeriesLast7Days = timeSeries7d,
                TopExecutedTasks = topExecutedTasks,
                TopFailingTasks = topFailingTasks,
                SlowestTasks = slowestTasks,
                Trends = trends,
                SystemHealth = systemHealth
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard analytics");
            throw;
        }
    }

    public async Task<List<ActiveExecutionInfo>> GetActiveExecutionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var activeMetrics = await _dbContext.ExecutionMetrics
                .Where(m => m.IsActive)
                .Include(m => m.Task)
                .OrderByDescending(m => m.RecordedAt)
                .ToListAsync(cancellationToken);

            return activeMetrics.Select(m => new ActiveExecutionInfo
            {
                TaskId = m.TaskId,
                ExecutionHistoryId = m.ExecutionHistoryId,
                TaskName = m.TaskName,
                CurrentStep = m.CurrentStep,
                TotalSteps = m.TotalSteps,
                CurrentAction = m.CurrentAction,
                CompletionPercentage = m.CompletionPercentage,
                DurationMs = m.DurationMs,
                StartedAt = m.RecordedAt,
                TargetUrl = m.TargetUrl
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active executions");
            throw;
        }
    }

    public async Task<List<TimeSeriesDataPoint>> GetTimeSeriesAsync(
        MetricType metricType,
        TimeInterval interval,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? taskId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.TimeSeriesData
                .Where(t => t.MetricType == metricType &&
                           t.Interval == interval &&
                           t.Timestamp >= startTime &&
                           t.Timestamp <= endTime);

            if (taskId.HasValue)
            {
                query = query.Where(t => t.TaskId == taskId.Value);
            }

            return await query
                .OrderBy(t => t.Timestamp)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get time series data");
            throw;
        }
    }

    public async Task CalculateTimeSeriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            
            // Calculate for different intervals
            await CalculateTimeSeriesForIntervalAsync(TimeInterval.Hour, now, cancellationToken);
            await CalculateTimeSeriesForIntervalAsync(TimeInterval.Day, now, cancellationToken);

            _logger.LogInformation("Time series calculation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate time series");
            throw;
        }
    }

    private static DateTimeOffset RoundToIntervalSafe(DateTimeOffset timestamp, TimeInterval interval)
    {
        switch (interval)
        {
            case TimeInterval.Hour:
                // Truncate to the start of the hour, preserving the original offset
                return new DateTimeOffset(
                    timestamp.Year,
                    timestamp.Month,
                    timestamp.Day,
                    timestamp.Hour,
                    0,
                    0,
                    timestamp.Offset);

            case TimeInterval.Day:
                // Truncate to the start of the day, preserving the original offset
                return new DateTimeOffset(
                    timestamp.Year,
                    timestamp.Month,
                    timestamp.Day,
                    0,
                    0,
                    0,
                    timestamp.Offset);

            case TimeInterval.Week:
                // Approximate the original weekly rounding while preserving offset.
                // Assuming Sunday as the start of the week to mirror typical DayOfWeek-based logic.
                var localDate = timestamp.Date; // DateTime (no offset)
                var weekStartLocal = localDate.AddDays(-(int)localDate.DayOfWeek);
                return new DateTimeOffset(weekStartLocal, timestamp.Offset);

            default:
                // For any other intervals, fall back to returning the original timestamp
                // to avoid introducing unexpected behavior.
                return timestamp;
        }
    }

    private async Task CalculateTimeSeriesForIntervalAsync(
        TimeInterval interval,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var roundedTimestamp = RoundToIntervalSafe(now, interval);
        var intervalStart = roundedTimestamp.Subtract(interval.GetDuration());

        // Check if we already have data for this interval
        var existingDataPoint = await _dbContext.TimeSeriesData
            .FirstOrDefaultAsync(
                t => t.Interval == interval &&
                     t.Timestamp == roundedTimestamp &&
                     t.MetricType == MetricType.SuccessRate,
                cancellationToken);

        if (existingDataPoint != null)
        {
            _logger.LogDebug("Time series data already exists for {Interval} at {Timestamp}", interval, roundedTimestamp);
            return;
        }

        // Get executions in this interval
        var executions = await _dbContext.ExecutionHistory
            .Where(h => h.StartedAt >= intervalStart && h.StartedAt < roundedTimestamp)
            .ToListAsync(cancellationToken);

        if (!executions.Any())
        {
            _logger.LogDebug("No executions found for {Interval} interval", interval);
            return;
        }

        var successCount = executions.Count(e => e.ExecutionStatus == ExecutionStatus.Success);
        var failureCount = executions.Count(e => e.ExecutionStatus == ExecutionStatus.Failed);
        var successRate = (double)successCount / executions.Count * 100;
        var avgDuration = (long)executions.Average(e => e.DurationMs);
        var minDuration = executions.Min(e => e.DurationMs);
        var maxDuration = executions.Max(e => e.DurationMs);

        // Create data points for different metric types
        var dataPoints = new[]
        {
            new TimeSeriesDataPoint
            {
                Timestamp = roundedTimestamp,
                Interval = interval,
                MetricType = MetricType.SuccessRate,
                Value = successRate,
                ExecutionCount = executions.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                SuccessRate = successRate,
                AverageDurationMs = avgDuration,
                MinDurationMs = minDuration,
                MaxDurationMs = maxDuration
            },
            new TimeSeriesDataPoint
            {
                Timestamp = roundedTimestamp,
                Interval = interval,
                MetricType = MetricType.ExecutionCount,
                Value = executions.Count,
                ExecutionCount = executions.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                SuccessRate = successRate,
                AverageDurationMs = avgDuration,
                MinDurationMs = minDuration,
                MaxDurationMs = maxDuration
            },
            new TimeSeriesDataPoint
            {
                Timestamp = roundedTimestamp,
                Interval = interval,
                MetricType = MetricType.AverageDuration,
                Value = avgDuration,
                ExecutionCount = executions.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                SuccessRate = successRate,
                AverageDurationMs = avgDuration,
                MinDurationMs = minDuration,
                MaxDurationMs = maxDuration
            }
        };

        _dbContext.TimeSeriesData.AddRange(dataPoints);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Created {Count} time series data points for {Interval} at {Timestamp}",
            dataPoints.Length,
            interval,
            roundedTimestamp);
    }

    public async Task<List<ExecutionMetrics>> GetTaskMetricsAsync(
        Guid taskId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.ExecutionMetrics
                .Where(m => m.TaskId == taskId);

            if (!includeInactive)
            {
                query = query.Where(m => m.IsActive);
            }

            return await query
                .OrderByDescending(m => m.RecordedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task metrics for Task {TaskId}", taskId);
            throw;
        }
    }

    public async Task CompleteExecutionAsync(
        Guid taskId,
        ExecutionStatus status,
        long durationMs,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the active metric for this task
            var activeMetric = await _dbContext.ExecutionMetrics
                .Where(m => m.TaskId == taskId && m.IsActive)
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeMetric == null)
            {
                _logger.LogWarning("No active execution metric found for Task {TaskId}", taskId);
                return;
            }

            // Update the metric
            activeMetric.IsActive = false;
            activeMetric.Status = status;
            activeMetric.DurationMs = durationMs;
            activeMetric.ErrorMessage = errorMessage;
            activeMetric.CompletionPercentage = status == ExecutionStatus.Success ? 100 : activeMetric.CompletionPercentage;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Completed execution for Task {TaskId} with status {Status} in {DurationMs}ms",
                taskId,
                status,
                durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete execution for Task {TaskId}", taskId);
            throw;
        }
    }

    public async Task UpdateExecutionProgressAsync(
        Guid taskId,
        int currentStep,
        string currentAction,
        long durationMs,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeMetric = await _dbContext.ExecutionMetrics
                .Where(m => m.TaskId == taskId && m.IsActive)
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeMetric == null)
            {
                _logger.LogWarning("No active execution metric found for Task {TaskId}", taskId);
                return;
            }

            activeMetric.CurrentStep = currentStep;
            activeMetric.CurrentAction = currentAction;
            activeMetric.DurationMs = durationMs;
            activeMetric.CompletionPercentage = activeMetric.TotalSteps > 0
                ? (double)currentStep / activeMetric.TotalSteps * 100
                : 0;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Updated execution progress for Task {TaskId}: Step {CurrentStep}/{TotalSteps}",
                taskId,
                currentStep,
                activeMetric.TotalSteps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update execution progress for Task {TaskId}", taskId);
            throw;
        }
    }

    public async Task<SystemHealthMetrics> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var last24Hours = DateTimeOffset.UtcNow.AddHours(-24);
            
            var recentExecutions = await _dbContext.ExecutionHistory
                .Where(h => h.StartedAt >= last24Hours)
                .ToListAsync(cancellationToken);

            if (!recentExecutions.Any())
            {
                return new SystemHealthMetrics
                {
                    Status = HealthStatus.Unknown,
                    ErrorRate = 0,
                    AverageResponseTimeMs = 0,
                    ConsecutiveFailures = 0,
                    UptimePercentage = 100,
                    HealthMessages = new List<string> { "Insufficient data" }
                };
            }

            var failureCount = recentExecutions.Count(e => e.ExecutionStatus == ExecutionStatus.Failed);
            var errorRate = (double)failureCount / recentExecutions.Count * 100;
            var avgResponseTime = (long)recentExecutions.Average(e => e.DurationMs);
            
            // Calculate consecutive failures
            var recentOrderedExecutions = await _dbContext.ExecutionHistory
                .OrderByDescending(h => h.StartedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            var consecutiveFailures = 0;
            foreach (var execution in recentOrderedExecutions)
            {
                if (execution.ExecutionStatus == ExecutionStatus.Failed)
                {
                    consecutiveFailures++;
                }
                else
                {
                    break;
                }
            }

            var uptimePercentage = 100 - errorRate;

            // Determine health status
            var status = errorRate switch
            {
                < 5 => HealthStatus.Healthy,
                < 15 => HealthStatus.Degraded,
                _ => HealthStatus.Unhealthy
            };

            var healthMessages = new List<string>();
            if (errorRate > 20)
            {
                healthMessages.Add($"High error rate: {errorRate:F1}%");
            }
            if (consecutiveFailures >= 3)
            {
                healthMessages.Add($"{consecutiveFailures} consecutive failures detected");
            }
            if (avgResponseTime > 30000)
            {
                healthMessages.Add($"High average response time: {avgResponseTime}ms");
            }
            if (!healthMessages.Any())
            {
                healthMessages.Add("System operating normally");
            }

            return new SystemHealthMetrics
            {
                Status = status,
                ErrorRate = errorRate,
                AverageResponseTimeMs = avgResponseTime,
                ConsecutiveFailures = consecutiveFailures,
                UptimePercentage = uptimePercentage,
                HealthMessages = healthMessages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health");
            throw;
        }
    }

    public async Task<List<TaskExecutionSummary>> GetTopExecutedTasksAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var taskSummaries = await _dbContext.ExecutionHistory
                .GroupBy(h => h.TaskId)
                .Select(g => new
                {
                    TaskId = g.Key,
                    ExecutionCount = g.Count(),
                    SuccessCount = g.Count(h => h.ExecutionStatus == ExecutionStatus.Success),
                    FailureCount = g.Count(h => h.ExecutionStatus == ExecutionStatus.Failed),
                    AverageDurationMs = (long)g.Average(h => h.DurationMs),
                    LastExecutedAt = g.Max(h => h.StartedAt)
                })
                .OrderByDescending(s => s.ExecutionCount)
                .Take(count)
                .ToListAsync(cancellationToken);

            var taskIds = taskSummaries.Select(s => s.TaskId).ToList();
            var tasks = await _dbContext.AutomationTasks
                .Where(t => taskIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

            return taskSummaries.Select(s => new TaskExecutionSummary
            {
                TaskId = s.TaskId,
                TaskName = tasks.TryGetValue(s.TaskId, out var task) ? task.Name : "Unknown",
                ExecutionCount = s.ExecutionCount,
                SuccessCount = s.SuccessCount,
                FailureCount = s.FailureCount,
                SuccessRate = s.ExecutionCount > 0 ? (double)s.SuccessCount / s.ExecutionCount * 100 : 0,
                AverageDurationMs = s.AverageDurationMs,
                LastExecutedAt = s.LastExecutedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top executed tasks");
            throw;
        }
    }

    public async Task<List<TaskExecutionSummary>> GetTopFailingTasksAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var taskSummaries = await _dbContext.ExecutionHistory
                .GroupBy(h => h.TaskId)
                .Select(g => new
                {
                    TaskId = g.Key,
                    ExecutionCount = g.Count(),
                    SuccessCount = g.Count(h => h.ExecutionStatus == ExecutionStatus.Success),
                    FailureCount = g.Count(h => h.ExecutionStatus == ExecutionStatus.Failed),
                    FailureRate = g.Count() > 0 ? (double)g.Count(h => h.ExecutionStatus == ExecutionStatus.Failed) / g.Count() * 100 : 0,
                    AverageDurationMs = (long)g.Average(h => h.DurationMs),
                    LastExecutedAt = g.Max(h => h.StartedAt)
                })
                .Where(s => s.FailureCount > 0)
                .OrderByDescending(s => s.FailureRate)
                .ThenByDescending(s => s.FailureCount)
                .Take(count)
                .ToListAsync(cancellationToken);

            var taskIds = taskSummaries.Select(s => s.TaskId).ToList();
            var tasks = await _dbContext.AutomationTasks
                .Where(t => taskIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

            return taskSummaries.Select(s => new TaskExecutionSummary
            {
                TaskId = s.TaskId,
                TaskName = tasks.TryGetValue(s.TaskId, out var task) ? task.Name : "Unknown",
                ExecutionCount = s.ExecutionCount,
                SuccessCount = s.SuccessCount,
                FailureCount = s.FailureCount,
                SuccessRate = s.ExecutionCount > 0 ? (double)s.SuccessCount / s.ExecutionCount * 100 : 0,
                AverageDurationMs = s.AverageDurationMs,
                LastExecutedAt = s.LastExecutedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top failing tasks");
            throw;
        }
    }

    public async Task<List<TaskExecutionSummary>> GetSlowestTasksAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var taskSummaries = await _dbContext.ExecutionHistory
                .GroupBy(h => h.TaskId)
                .Select(g => new
                {
                    TaskId = g.Key,
                    ExecutionCount = g.Count(),
                    SuccessCount = g.Count(h => h.ExecutionStatus == ExecutionStatus.Success),
                    FailureCount = g.Count(h => h.ExecutionStatus == ExecutionStatus.Failed),
                    AverageDurationMs = (long)g.Average(h => h.DurationMs),
                    LastExecutedAt = g.Max(h => h.StartedAt)
                })
                .OrderByDescending(s => s.AverageDurationMs)
                .Take(count)
                .ToListAsync(cancellationToken);

            var taskIds = taskSummaries.Select(s => s.TaskId).ToList();
            var tasks = await _dbContext.AutomationTasks
                .Where(t => taskIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken);

            return taskSummaries.Select(s => new TaskExecutionSummary
            {
                TaskId = s.TaskId,
                TaskName = tasks.TryGetValue(s.TaskId, out var task) ? task.Name : "Unknown",
                ExecutionCount = s.ExecutionCount,
                SuccessCount = s.SuccessCount,
                FailureCount = s.FailureCount,
                SuccessRate = s.ExecutionCount > 0 ? (double)s.SuccessCount / s.ExecutionCount * 100 : 0,
                AverageDurationMs = s.AverageDurationMs,
                LastExecutedAt = s.LastExecutedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get slowest tasks");
            throw;
        }
    }

    public async Task<ExecutionTrends> CalculateTrendsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var currentPeriodStart = now.AddHours(-24);
            var previousPeriodStart = now.AddHours(-48);
            var previousPeriodEnd = now.AddHours(-24);

            var currentPeriodExecutions = await _dbContext.ExecutionHistory
                .Where(h => h.StartedAt >= currentPeriodStart)
                .ToListAsync(cancellationToken);

            var previousPeriodExecutions = await _dbContext.ExecutionHistory
                .Where(h => h.StartedAt >= previousPeriodStart && h.StartedAt < previousPeriodEnd)
                .ToListAsync(cancellationToken);

            var currentSuccessRate = CalculateSuccessRate(currentPeriodExecutions);
            var previousSuccessRate = CalculateSuccessRate(previousPeriodExecutions);

            var currentVolume = currentPeriodExecutions.Count;
            var previousVolume = previousPeriodExecutions.Count;

            var currentAvgDuration = currentPeriodExecutions.Any()
                ? currentPeriodExecutions.Average(e => e.DurationMs)
                : 0;

            var previousAvgDuration = previousPeriodExecutions.Any()
                ? previousPeriodExecutions.Average(e => e.DurationMs)
                : 0;

            return new ExecutionTrends
            {
                SuccessRateTrend = DetermineTrend(currentSuccessRate, previousSuccessRate),
                ExecutionVolumeTrend = DetermineTrend(currentVolume, previousVolume),
                DurationTrend = DetermineTrend(previousAvgDuration, currentAvgDuration), // Inverted: lower is better
                SuccessRateChangePercent = CalculateChangePercent(currentSuccessRate, previousSuccessRate),
                ExecutionVolumeChangePercent = CalculateChangePercent(currentVolume, previousVolume),
                DurationChangePercent = CalculateChangePercent(currentAvgDuration, previousAvgDuration)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate trends");
            throw;
        }
    }

    private static double CalculateSuccessRate(IEnumerable<ExecutionHistory> executions)
    {
        var executionsList = executions.ToList();
        if (!executionsList.Any())
        {
            return 0;
        }

        var successCount = executionsList.Count(e => e.ExecutionStatus == ExecutionStatus.Success);
        return (double)successCount / executionsList.Count * 100;
    }

    private static TrendDirection DetermineTrend(double current, double previous)
    {
        const double epsilon = 1e-10;
        
        if (Math.Abs(previous) < epsilon)
        {
            return TrendDirection.Unknown;
        }

        var changePercent = Math.Abs((current - previous) / previous * 100);
        
        // Consider less than 5% change as stable
        if (changePercent < 5)
        {
            return TrendDirection.Stable;
        }

        return current > previous ? TrendDirection.Up : TrendDirection.Down;
    }

    private static double CalculateChangePercent(double current, double previous)
    {
        const double epsilon = 1e-10;
        
        if (Math.Abs(previous) < epsilon)
        {
            return Math.Abs(current) > epsilon ? 100 : 0;
        }

        return (current - previous) / previous * 100;
    }
}
