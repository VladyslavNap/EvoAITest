namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Real-time dashboard analytics aggregation.
/// Provides comprehensive metrics for the dashboard view including active executions,
/// success rates, and performance statistics.
/// </summary>
public sealed class DashboardAnalytics
{
    /// <summary>
    /// Gets or sets the timestamp when these analytics were calculated.
    /// </summary>
    public DateTimeOffset CalculatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the currently active executions.
    /// </summary>
    public List<ActiveExecutionInfo> ActiveExecutions { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of active executions.
    /// </summary>
    public int ActiveExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions in the last 24 hours.
    /// </summary>
    public int ExecutionsLast24Hours { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions in the last hour.
    /// </summary>
    public int ExecutionsLastHour { get; set; }

    /// <summary>
    /// Gets or sets the success rate for the last 24 hours (0-100).
    /// </summary>
    public double SuccessRateLast24Hours { get; set; }

    /// <summary>
    /// Gets or sets the success rate for the last hour (0-100).
    /// </summary>
    public double SuccessRateLastHour { get; set; }

    /// <summary>
    /// Gets or sets the average execution duration in milliseconds (last 24 hours).
    /// </summary>
    public long AverageDurationMsLast24Hours { get; set; }

    /// <summary>
    /// Gets or sets the average execution duration in milliseconds (last hour).
    /// </summary>
    public long AverageDurationMsLastHour { get; set; }

    /// <summary>
    /// Gets or sets the total number of tasks executed today.
    /// </summary>
    public int TotalExecutionsToday { get; set; }

    /// <summary>
    /// Gets or sets the total number of successful executions today.
    /// </summary>
    public int SuccessfulExecutionsToday { get; set; }

    /// <summary>
    /// Gets or sets the total number of failed executions today.
    /// </summary>
    public int FailedExecutionsToday { get; set; }

    /// <summary>
    /// Gets or sets the success rate for today (0-100).
    /// </summary>
    public double SuccessRateToday { get; set; }

    /// <summary>
    /// Gets or sets the total number of unique tasks.
    /// </summary>
    public int TotalTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of tasks with healing enabled.
    /// </summary>
    public int TasksWithHealingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the healing success rate (0-100).
    /// </summary>
    public double HealingSuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the time series data for the last 24 hours.
    /// </summary>
    public List<TimeSeriesDataPoint> TimeSeriesLast24Hours { get; set; } = [];

    /// <summary>
    /// Gets or sets the time series data for the last 7 days.
    /// </summary>
    public List<TimeSeriesDataPoint> TimeSeriesLast7Days { get; set; } = [];

    /// <summary>
    /// Gets or sets the most frequently executed tasks.
    /// </summary>
    public List<TaskExecutionSummary> TopExecutedTasks { get; set; } = [];

    /// <summary>
    /// Gets or sets the tasks with the highest failure rates.
    /// </summary>
    public List<TaskExecutionSummary> TopFailingTasks { get; set; } = [];

    /// <summary>
    /// Gets or sets the slowest executing tasks.
    /// </summary>
    public List<TaskExecutionSummary> SlowestTasks { get; set; } = [];

    /// <summary>
    /// Gets or sets recent execution trends.
    /// </summary>
    public ExecutionTrends Trends { get; set; } = new();

    /// <summary>
    /// Gets or sets system health indicators.
    /// </summary>
    public SystemHealthMetrics SystemHealth { get; set; } = new();
}

/// <summary>
/// Information about an active execution.
/// </summary>
public sealed class ActiveExecutionInfo
{
    /// <summary>
    /// Gets or sets the task ID.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Gets or sets the execution history ID.
    /// </summary>
    public Guid? ExecutionHistoryId { get; init; }

    /// <summary>
    /// Gets or sets the task name.
    /// </summary>
    public required string TaskName { get; init; }

    /// <summary>
    /// Gets or sets the current step number.
    /// </summary>
    public int CurrentStep { get; init; }

    /// <summary>
    /// Gets or sets the total number of steps.
    /// </summary>
    public int TotalSteps { get; init; }

    /// <summary>
    /// Gets or sets the current action being performed.
    /// </summary>
    public string? CurrentAction { get; init; }

    /// <summary>
    /// Gets or sets the completion percentage (0-100).
    /// </summary>
    public double CompletionPercentage { get; init; }

    /// <summary>
    /// Gets or sets the duration in milliseconds so far.
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Gets or sets when the execution started.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets or sets the target URL being tested.
    /// </summary>
    public string? TargetUrl { get; init; }
}

/// <summary>
/// Summary of task execution metrics.
/// </summary>
public sealed class TaskExecutionSummary
{
    /// <summary>
    /// Gets or sets the task ID.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Gets or sets the task name.
    /// </summary>
    public required string TaskName { get; init; }

    /// <summary>
    /// Gets or sets the total execution count.
    /// </summary>
    public int ExecutionCount { get; init; }

    /// <summary>
    /// Gets or sets the successful execution count.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Gets or sets the failed execution count.
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// Gets or sets the success rate (0-100).
    /// </summary>
    public double SuccessRate { get; init; }

    /// <summary>
    /// Gets or sets the average duration in milliseconds.
    /// </summary>
    public long AverageDurationMs { get; init; }

    /// <summary>
    /// Gets or sets the last execution timestamp.
    /// </summary>
    public DateTimeOffset? LastExecutedAt { get; init; }
}

/// <summary>
/// Execution trends over time.
/// </summary>
public sealed class ExecutionTrends
{
    /// <summary>
    /// Gets or sets the trend direction for success rate.
    /// </summary>
    public TrendDirection SuccessRateTrend { get; set; }

    /// <summary>
    /// Gets or sets the trend direction for execution volume.
    /// </summary>
    public TrendDirection ExecutionVolumeTrend { get; set; }

    /// <summary>
    /// Gets or sets the trend direction for average duration.
    /// </summary>
    public TrendDirection DurationTrend { get; set; }

    /// <summary>
    /// Gets or sets the percentage change in success rate compared to previous period.
    /// </summary>
    public double SuccessRateChangePercent { get; set; }

    /// <summary>
    /// Gets or sets the percentage change in execution volume compared to previous period.
    /// </summary>
    public double ExecutionVolumeChangePercent { get; set; }

    /// <summary>
    /// Gets or sets the percentage change in average duration compared to previous period.
    /// </summary>
    public double DurationChangePercent { get; set; }
}

/// <summary>
/// Trend direction indicator.
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Trending upward.
    /// </summary>
    Up,

    /// <summary>
    /// Trending downward.
    /// </summary>
    Down,

    /// <summary>
    /// Stable, no significant change.
    /// </summary>
    Stable,

    /// <summary>
    /// Insufficient data to determine trend.
    /// </summary>
    Unknown
}

/// <summary>
/// System health metrics for monitoring.
/// </summary>
public sealed class SystemHealthMetrics
{
    /// <summary>
    /// Gets or sets the overall system health status.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the current error rate (0-100).
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public long AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets the uptime percentage (0-100).
    /// </summary>
    public double UptimePercentage { get; set; }

    /// <summary>
    /// Gets or sets health check messages.
    /// </summary>
    public List<string> HealthMessages { get; set; } = [];
}

/// <summary>
/// Health status indicator.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// System is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// System is degraded but operational.
    /// </summary>
    Degraded,

    /// <summary>
    /// System is unhealthy.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Health status unknown.
    /// </summary>
    Unknown
}
