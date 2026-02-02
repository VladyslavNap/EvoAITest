using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace EvoAITest.ApiService.Controllers;

/// <summary>
/// API controller for real-time execution analytics and metrics.
/// </summary>
[ApiController]
[Route("api/execution-analytics")]
[Produces("application/json")]
public sealed class ExecutionAnalyticsController : ControllerBase
{
    // Response cache durations in seconds
    private const int CacheDurationShort = 10;
    private const int CacheDurationMedium = 30;
    private const int CacheDurationLong = 60;
    private const int CacheDurationExtended = 300;

    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<ExecutionAnalyticsController> _logger;

    public ExecutionAnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<ExecutionAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets comprehensive dashboard analytics with real-time execution metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard analytics with real-time metrics.</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = CacheDurationShort)]
    public async Task<ActionResult<DashboardAnalytics>> GetDashboardAnalytics(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting dashboard analytics");
            var analytics = await _analyticsService.GetDashboardAnalyticsAsync(cancellationToken);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard analytics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve dashboard analytics" });
        }
    }

    /// <summary>
    /// Gets currently active executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active execution information.</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<ActiveExecutionInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ActiveExecutionInfo>>> GetActiveExecutions(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting active executions");
            var activeExecutions = await _analyticsService.GetActiveExecutionsAsync(cancellationToken);
            return Ok(activeExecutions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active executions");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve active executions" });
        }
    }

    /// <summary>
    /// Gets time series data for a specific metric type and interval.
    /// </summary>
    /// <param name="metricType">The type of metric to retrieve (default: SuccessRate).</param>
    /// <param name="interval">The time interval for aggregation (default: Hour).</param>
    /// <param name="hours">Number of hours to look back (default: 24, max: 720).</param>
    /// <param name="taskId">Optional task ID to filter by specific task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of time series data points.</returns>
    [HttpGet("time-series")]
    [ProducesResponseType(typeof(List<TimeSeriesDataPoint>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = CacheDurationLong)]
    public async Task<ActionResult<List<TimeSeriesDataPoint>>> GetTimeSeries(
        [FromQuery] MetricType metricType = MetricType.SuccessRate,
        [FromQuery] TimeInterval interval = TimeInterval.Hour,
        [FromQuery] int hours = 24,
        [FromQuery] Guid? taskId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (hours < 1 || hours > 720) // Max 30 days
            {
                return BadRequest(new { error = "Hours must be between 1 and 720" });
            }

            _logger.LogDebug(
                "Getting time series: MetricType={MetricType}, Interval={Interval}, Hours={Hours}, TaskId={TaskId}",
                metricType,
                interval,
                hours,
                taskId);

            var endTime = DateTimeOffset.UtcNow;
            var startTime = endTime.AddHours(-hours);

            var timeSeries = await _analyticsService.GetTimeSeriesAsync(
                metricType,
                interval,
                startTime,
                endTime,
                taskId,
                cancellationToken);

            return Ok(timeSeries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get time series data");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve time series data" });
        }
    }

    /// <summary>
    /// Gets execution metrics for a specific task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="includeInactive">Whether to include inactive (completed) metrics (default: false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution metrics for the specified task.</returns>
    [HttpGet("tasks/{taskId:guid}/metrics")]
    [ProducesResponseType(typeof(List<ExecutionMetrics>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ExecutionMetrics>>> GetTaskMetrics(
        Guid taskId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting metrics for Task {TaskId}", taskId);
            var metrics = await _analyticsService.GetTaskMetricsAsync(
                taskId,
                includeInactive,
                cancellationToken);

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task metrics for Task {TaskId}", taskId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = $"Failed to retrieve metrics for task {taskId}" });
        }
    }

    /// <summary>
    /// Gets system health metrics and status indicators.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>System health metrics including error rates and uptime.</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = CacheDurationMedium)]
    public async Task<ActionResult<SystemHealthMetrics>> GetSystemHealth(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting system health metrics");
            var health = await _analyticsService.GetSystemHealthAsync(cancellationToken);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve system health" });
        }
    }

    /// <summary>
    /// Gets the most frequently executed tasks.
    /// </summary>
    /// <param name="count">Number of tasks to return (default: 10, max: 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of task execution summaries ordered by execution count.</returns>
    [HttpGet("top-executed")]
    [ProducesResponseType(typeof(List<TaskExecutionSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = CacheDurationLong)]
    public async Task<ActionResult<List<TaskExecutionSummary>>> GetTopExecutedTasks(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (count < 1 || count > 50)
            {
                return BadRequest(new { error = "Count must be between 1 and 50" });
            }

            _logger.LogDebug("Getting top {Count} executed tasks", count);
            var tasks = await _analyticsService.GetTopExecutedTasksAsync(count, cancellationToken);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top executed tasks");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve top executed tasks" });
        }
    }

    /// <summary>
    /// Gets tasks with the highest failure rates.
    /// </summary>
    /// <param name="count">Number of tasks to return (default: 10, max: 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of task execution summaries ordered by failure rate.</returns>
    [HttpGet("top-failing")]
    [ProducesResponseType(typeof(List<TaskExecutionSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = CacheDurationLong)]
    public async Task<ActionResult<List<TaskExecutionSummary>>> GetTopFailingTasks(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (count < 1 || count > 50)
            {
                return BadRequest(new { error = "Count must be between 1 and 50" });
            }

            _logger.LogDebug("Getting top {Count} failing tasks", count);
            var tasks = await _analyticsService.GetTopFailingTasksAsync(count, cancellationToken);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top failing tasks");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve top failing tasks" });
        }
    }

    /// <summary>
    /// Gets tasks with the slowest average execution times.
    /// </summary>
    /// <param name="count">Number of tasks to return (default: 10, max: 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of task execution summaries ordered by average duration.</returns>
    [HttpGet("slowest")]
    [ProducesResponseType(typeof(List<TaskExecutionSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = CacheDurationLong)]
    public async Task<ActionResult<List<TaskExecutionSummary>>> GetSlowestTasks(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (count < 1 || count > 50)
            {
                return BadRequest(new { error = "Count must be between 1 and 50" });
            }

            _logger.LogDebug("Getting {Count} slowest tasks", count);
            var tasks = await _analyticsService.GetSlowestTasksAsync(count, cancellationToken);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get slowest tasks");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve slowest tasks" });
        }
    }

    /// <summary>
    /// Gets execution trends comparing current period to previous period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution trends including success rate, volume, and duration changes.</returns>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(ExecutionTrends), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = CacheDurationExtended)]
    public async Task<ActionResult<ExecutionTrends>> GetTrends(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting execution trends");
            var trends = await _analyticsService.CalculateTrendsAsync(cancellationToken);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution trends");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve execution trends" });
        }
    }

    /// <summary>
    /// Triggers calculation of time series data points.
    /// This is typically called by a background job but can be manually triggered.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Accepted status with calculation initiated message.</returns>
    [HttpPost("calculate-time-series")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult CalculateTimeSeries(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting time series calculation");
            
            // Run in background without waiting - use a separate CancellationToken to avoid request cancellation affecting the background task
            _ = Task.Run(async () =>
            {
                try
                {
                    // Create a new CancellationTokenSource for the background operation
                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                    await _analyticsService.CalculateTimeSeriesAsync(cts.Token);
                    _logger.LogInformation("Time series calculation completed successfully");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Time series calculation was cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Time series calculation failed in background");
                }
            }, CancellationToken.None); // Don't link to request cancellation token

            return Accepted(new { message = "Time series calculation started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start time series calculation");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to start time series calculation" });
        }
    }
}
