using EvoAITest.Core.Models;
using EvoAITest.Core.Models.Analytics;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for collecting, aggregating, and analyzing execution metrics.
/// Provides real-time analytics for dashboard visualization and monitoring.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Records execution metrics for a task.
    /// </summary>
    /// <param name="metrics">The execution metrics to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordMetricsAsync(ExecutionMetrics metrics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current dashboard analytics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard analytics with real-time metrics.</returns>
    Task<DashboardAnalytics> GetDashboardAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets currently active executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active execution information.</returns>
    Task<List<ActiveExecutionInfo>> GetActiveExecutionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets time series data for a specific metric type and interval.
    /// </summary>
    /// <param name="metricType">The type of metric to retrieve.</param>
    /// <param name="interval">The time interval for aggregation.</param>
    /// <param name="startTime">The start time for the time series.</param>
    /// <param name="endTime">The end time for the time series.</param>
    /// <param name="taskId">Optional task ID to filter by specific task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of time series data points.</returns>
    Task<List<TimeSeriesDataPoint>> GetTimeSeriesAsync(
        MetricType metricType,
        TimeInterval interval,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? taskId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates and stores time series data points.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CalculateTimeSeriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution metrics for a specific task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="includeInactive">Whether to include inactive metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution metrics.</returns>
    Task<List<ExecutionMetrics>> GetTaskMetricsAsync(
        Guid taskId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an execution as complete and records final metrics.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="status">The final execution status.</param>
    /// <param name="durationMs">Total execution duration in milliseconds.</param>
    /// <param name="errorMessage">Error message if execution failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompleteExecutionAsync(
        Guid taskId,
        ExecutionStatus status,
        long durationMs,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates execution progress in real-time.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="currentStep">The current step number.</param>
    /// <param name="currentAction">The current action being performed.</param>
    /// <param name="durationMs">Current execution duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateExecutionProgressAsync(
        Guid taskId,
        int currentStep,
        string currentAction,
        long durationMs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates step counts (completed/failed) for an active execution.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="success">Whether the step succeeded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateStepCountsAsync(
        Guid taskId,
        bool success,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system health metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>System health metrics.</returns>
    Task<SystemHealthMetrics> GetSystemHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top executed tasks.
    /// </summary>
    /// <param name="count">Number of tasks to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of task execution summaries.</returns>
    Task<List<TaskExecutionSummary>> GetTopExecutedTasksAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top failing tasks.
    /// </summary>
    /// <param name="count">Number of tasks to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of task execution summaries.</returns>
    Task<List<TaskExecutionSummary>> GetTopFailingTasksAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets slowest tasks.
    /// </summary>
    /// <param name="count">Number of tasks to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of task execution summaries.</returns>
    Task<List<TaskExecutionSummary>> GetSlowestTasksAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates execution trends comparing current period to previous period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution trends.</returns>
    Task<ExecutionTrends> CalculateTrendsAsync(CancellationToken cancellationToken = default);
}
