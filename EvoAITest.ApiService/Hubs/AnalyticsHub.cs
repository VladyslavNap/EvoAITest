using EvoAITest.Core.Models.Analytics;
using Microsoft.AspNetCore.SignalR;

namespace EvoAITest.ApiService.Hubs;

/// <summary>
/// SignalR hub for real-time analytics updates
/// </summary>
public sealed class AnalyticsHub : Hub
{
    private readonly ILogger<AnalyticsHub> _logger;

    public AnalyticsHub(ILogger<AnalyticsHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Client connected to AnalyticsHub: ConnectionId={ConnectionId}",
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Client disconnected from AnalyticsHub: ConnectionId={ConnectionId}",
            Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to dashboard updates
    /// </summary>
    public async Task SubscribeToDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Dashboard");
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to Dashboard updates",
            Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from dashboard updates
    /// </summary>
    public async Task UnsubscribeFromDashboard()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Dashboard");
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from Dashboard updates",
            Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to recording-specific updates
    /// </summary>
    public async Task SubscribeToRecording(Guid recordingId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Recording_{recordingId}");
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to Recording {RecordingId} updates",
            Context.ConnectionId,
            recordingId);
    }

    /// <summary>
    /// Unsubscribe from recording-specific updates
    /// </summary>
    public async Task UnsubscribeFromRecording(Guid recordingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Recording_{recordingId}");
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from Recording {RecordingId} updates",
            Context.ConnectionId,
            recordingId);
    }

    /// <summary>
    /// Subscribe to task execution updates
    /// </summary>
    public async Task SubscribeToTask(Guid taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Task_{taskId}");
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to Task {TaskId} updates",
            Context.ConnectionId,
            taskId);
    }

    /// <summary>
    /// Unsubscribe from task execution updates
    /// </summary>
    public async Task UnsubscribeFromTask(Guid taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Task_{taskId}");
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from Task {TaskId} updates",
            Context.ConnectionId,
            taskId);
    }

    /// <summary>
    /// Subscribe to real-time metrics
    /// </summary>
    public async Task SubscribeToMetrics()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Metrics");
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to Metrics updates",
            Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from real-time metrics
    /// </summary>
    public async Task UnsubscribeFromMetrics()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Metrics");
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from Metrics updates",
            Context.ConnectionId);
    }
}

/// <summary>
/// Extension methods for sending analytics updates via SignalR
/// </summary>
public static class AnalyticsHubExtensions
{
    /// <summary>
    /// Sends dashboard statistics update to all subscribed clients
    /// </summary>
    public static async Task SendDashboardUpdate(
        this IHubContext<AnalyticsHub> hubContext,
        DashboardStatistics statistics)
    {
        await hubContext.Clients.Group("Dashboard").SendAsync(
            "DashboardUpdated",
            statistics);
    }

    /// <summary>
    /// Sends flaky test detection notification
    /// </summary>
    public static async Task SendFlakyTestDetected(
        this IHubContext<AnalyticsHub> hubContext,
        FlakyTestAnalysis analysis)
    {
        await hubContext.Clients.Group("Dashboard").SendAsync(
            "FlakyTestDetected",
            analysis);
    }

    /// <summary>
    /// Sends test execution completion notification
    /// </summary>
    public static async Task SendTestExecutionCompleted(
        this IHubContext<AnalyticsHub> hubContext,
        Guid recordingId,
        string testName,
        bool passed)
    {
        await hubContext.Clients.Group($"Recording_{recordingId}").SendAsync(
            "TestExecutionCompleted",
            new
            {
                RecordingId = recordingId,
                TestName = testName,
                Passed = passed,
                Timestamp = DateTimeOffset.UtcNow
            });
    }

    /// <summary>
    /// Sends trend calculation completed notification
    /// </summary>
    public static async Task SendTrendCalculated(
        this IHubContext<AnalyticsHub> hubContext,
        List<TestTrend> trends)
    {
        await hubContext.Clients.Group("Dashboard").SendAsync(
            "TrendCalculated",
            trends);
    }

    /// <summary>
    /// Sends recording insights update
    /// </summary>
    public static async Task SendRecordingInsightsUpdate(
        this IHubContext<AnalyticsHub> hubContext,
        Guid recordingId,
        object insights)
    {
        await hubContext.Clients.Group($"Recording_{recordingId}").SendAsync(
            "RecordingInsightsUpdated",
            insights);
    }

    /// <summary>
    /// Sends pass rate alert when threshold is crossed
    /// </summary>
    public static async Task SendPassRateAlert(
        this IHubContext<AnalyticsHub> hubContext,
        double currentPassRate,
        double threshold,
        bool isBelow)
    {
        await hubContext.Clients.Group("Dashboard").SendAsync(
            "PassRateAlert",
            new
            {
                CurrentPassRate = currentPassRate,
                Threshold = threshold,
                IsBelow = isBelow,
                Message = isBelow
                    ? $"Pass rate dropped to {currentPassRate:F1}% (below threshold {threshold}%)"
                    : $"Pass rate recovered to {currentPassRate:F1}% (above threshold {threshold}%)",
                Timestamp = DateTimeOffset.UtcNow
            });
    }

        /// <summary>
        /// Broadcasts a general analytics notification
        /// </summary>
        public static async Task BroadcastNotification(
            this IHubContext<AnalyticsHub> hubContext,
            string title,
            string message,
            string type = "info")
        {
            await hubContext.Clients.All.SendAsync(
                "Notification",
                new
                {
                    Title = title,
                    Message = message,
                    Type = type, // info, success, warning, danger
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends health score update
        /// </summary>
        public static async Task SendHealthScoreUpdate(
            this IHubContext<AnalyticsHub> hubContext,
            TestSuiteHealth previousHealth,
            TestSuiteHealth currentHealth,
            double score)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "HealthScoreUpdated",
                new
                {
                    PreviousHealth = previousHealth.ToString(),
                    CurrentHealth = currentHealth.ToString(),
                    Score = score,
                    Changed = previousHealth != currentHealth,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends period comparison results
        /// </summary>
        public static async Task SendComparisonUpdate(
            this IHubContext<AnalyticsHub> hubContext,
            string comparisonType,
            double currentValue,
            double previousValue,
            string verdict)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "ComparisonUpdated",
                new
                {
                    Type = comparisonType, // "week-over-week", "month-over-month"
                    CurrentValue = currentValue,
                    PreviousValue = previousValue,
                    Change = currentValue - previousValue,
                    ChangePercent = previousValue > 0 ? ((currentValue - previousValue) / previousValue * 100) : 0,
                    Verdict = verdict,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends cache invalidation notification (for debugging)
        /// </summary>
        public static async Task SendCacheInvalidated(
            this IHubContext<AnalyticsHub> hubContext,
            string cacheKey,
            string reason)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "CacheInvalidated",
                new
                {
                    CacheKey = cacheKey,
                    Reason = reason,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends background job completion notification
        /// </summary>
        public static async Task SendBackgroundJobCompleted(
            this IHubContext<AnalyticsHub> hubContext,
            string jobName,
            bool success,
            int itemsProcessed,
            long durationMs)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "BackgroundJobCompleted",
                new
                {
                    JobName = jobName,
                    Success = success,
                    ItemsProcessed = itemsProcessed,
                    DurationMs = durationMs,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        // ===== Real-time Execution Tracking Methods =====

        /// <summary>
        /// Sends execution started notification
        /// </summary>
        public static async Task SendExecutionStarted(
            this IHubContext<AnalyticsHub> hubContext,
            ExecutionMetrics metrics)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "ExecutionStarted",
                new
                {
                    metrics.TaskId,
                    metrics.TaskName,
                    metrics.TotalSteps,
                    metrics.TargetUrl,
                    StartedAt = metrics.RecordedAt,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends execution progress update
        /// </summary>
        public static async Task SendExecutionProgress(
            this IHubContext<AnalyticsHub> hubContext,
            ExecutionMetrics metrics)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "ExecutionProgress",
                new
                {
                    metrics.TaskId,
                    metrics.TaskName,
                    metrics.CurrentStep,
                    metrics.TotalSteps,
                    metrics.CurrentAction,
                    metrics.CompletionPercentage,
                    metrics.DurationMs,
                    metrics.StepsCompleted,
                    metrics.StepsFailed,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends execution completed notification
        /// </summary>
        public static async Task SendExecutionCompleted(
            this IHubContext<AnalyticsHub> hubContext,
            ExecutionMetrics metrics)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "ExecutionCompleted",
                new
                {
                    metrics.TaskId,
                    metrics.TaskName,
                    metrics.Status,
                    metrics.DurationMs,
                    metrics.StepsCompleted,
                    metrics.StepsFailed,
                    metrics.ErrorMessage,
                    metrics.HealingAttempted,
                    metrics.HealingSuccessful,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends real-time dashboard analytics update
        /// </summary>
        public static async Task SendDashboardAnalytics(
            this IHubContext<AnalyticsHub> hubContext,
            DashboardAnalytics analytics)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "DashboardAnalyticsUpdated",
                analytics);
        }

        /// <summary>
        /// Sends active executions update
        /// </summary>
        public static async Task SendActiveExecutionsUpdate(
            this IHubContext<AnalyticsHub> hubContext,
            List<ActiveExecutionInfo> activeExecutions)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "ActiveExecutionsUpdated",
                new
                {
                    ActiveExecutions = activeExecutions,
                    Count = activeExecutions.Count,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends time series data update
        /// </summary>
        public static async Task SendTimeSeriesUpdate(
            this IHubContext<AnalyticsHub> hubContext,
            List<TimeSeriesDataPoint> dataPoints,
            TimeInterval interval)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "TimeSeriesUpdated",
                new
                {
                    DataPoints = dataPoints,
                    Interval = interval.ToString(),
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends system health update
        /// </summary>
        public static async Task SendSystemHealthUpdate(
            this IHubContext<AnalyticsHub> hubContext,
            SystemHealthMetrics health)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "SystemHealthUpdated",
                new
                {
                    health.Status,
                    health.ErrorRate,
                    health.AverageResponseTimeMs,
                    health.ConsecutiveFailures,
                    health.UptimePercentage,
                    health.HealthMessages,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        /// Sends execution metric update (individual metric point)
        /// </summary>
        public static async Task SendMetricUpdate(
            this IHubContext<AnalyticsHub> hubContext,
            string metricName,
            double value,
            Dictionary<string, object>? tags = null)
        {
            await hubContext.Clients.Group("Dashboard").SendAsync(
                "MetricUpdated",
                new
                {
                    MetricName = metricName,
                    Value = value,
                    Tags = tags ?? new Dictionary<string, object>(),
                    Timestamp = DateTimeOffset.UtcNow
                });
        }
    }

